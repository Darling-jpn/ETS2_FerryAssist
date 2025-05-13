using ETS2_FerryAssist.Core.Configuration;
using ETS2_FerryAssist.Core.Database;
using ETS2_FerryAssist.Core.Services.Speech;
using ETS2_FerryAssist.Core.Services.VoiceVox;
using ETS2_FerryAssist.Core.Utils;
using ETS2_FerryAssist.Forms;
using SCSSdkClient;
using SCSSdkClient.Object;
using System.Runtime.InteropServices;

namespace ETS2_FerryAssist;
/// <summary>
/// ETS2 FeryAssistのエントリーポイントを提供するクラス。
/// 主にアプリケーションの初期設定、音声合成の初期化、コンソールアプリケーションの実行、ジョブ情報の管理、音声入力によるインタラクションの処理を行う。
/// </summary>
public partial class Program
{
    // 多重起動防止用のミューテックス
    private static Mutex? _mutex;
    private const string MutexName = "Global\\ETS2_FerryAssist_Instance";

    // アプリケーション状態
    private static bool _lastCargoLoaded = false;
    private static bool _isRunning = true;
    private static bool _jobActive = false;
    private static bool _telemetryReceived = false;
    private static string? _lastCitySource;
    private static string? _lastCityDestination;

    // システムコンポーネント
    private static readonly DatabaseHelper _dbHelper;
    private static VoiceVoxClient? _voiceVoxClient;
    private static readonly SpeechRecognitionHelper _speechRecognizer = new();
    private static SCSSdkTelemetry? _telemetry;
    private static Thread? _consoleThread;
    private static MainForm? _mainForm;

    // データベースパス
    private static readonly string DatabasePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Database", "ferry_routes.db");

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    static Program()
    {
        if (!File.Exists(DatabasePath))
        {
            throw new FileNotFoundException(
                $"データベースファイルが見つかりません: {DatabasePath}",
                DatabasePath);
        }
        _dbHelper = new DatabaseHelper(DatabasePath);
    }

    [STAThread]
    public static void Main()
    {
        try
        {
            // 多重起動チェック
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "アプリケーションは既に実行されています。",
                    "ETS2フェリー案内システム",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // コンソールの初期化 - 最初は非表示
            ConsoleHelper.InitializeConsole();
            ConsoleHelper.HideConsole();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 初期設定の確認
            if (!InitializeSettings())
                return;

            // VOICEVOXの初期化
            if (!InitializeVoiceVox())
                return;

            // デバッグモードに応じてコンソール表示を制御
            if (GlobalConfig.Application.DebugMode)
                ConsoleHelper.ShowDebugConsole();

            // メインフォームの作成とコンソールスレッドの開始
            _mainForm = new MainForm();
            _consoleThread = new Thread(RunConsoleApp);
            _consoleThread.Start();

            // メインフォームの実行
            Application.Run(_mainForm);
        }
        catch (Exception ex)
        {
            LogError("アプリ起動エラー", ex);
            MessageBox.Show($"アプリ起動エラー:\n{ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CleanupResources();
        }
    }

    // 初期設定の確認（初回起動時の設定画面表示）
    private static bool InitializeSettings()
    {
        // 初回起動時のみ設定画面を表示
        bool isFirstRun = !GlobalConfig.VoiceVox.HasVoiceVoxPath;
        if (isFirstRun)
        {
            using var settingsForm = new SettingsForm();
            MessageBox.Show(
                "初回起動時の設定です。\nVOICEVOXのパスを設定してください。",
                "初期設定",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            settingsForm.DisableCancelButton();

            if (settingsForm.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(
                    "初期設定が完了していないため、アプリケーションを終了します。",
                    "設定エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }
        return true;
    }

    // VOICEVOXの初期化
    private static bool InitializeVoiceVox()
    {
        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                string path = GlobalConfig.VoiceVoxPath;

                // 実行ファイル名チェック
                if (!Path.GetFileName(path).Equals("VOICEVOX.exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "選択されたファイルは VOICEVOX.exe ではありません。\n再度正しいファイルを選択してください。",
                        "VOICEVOXの実行ファイルエラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    using var ofd = new OpenFileDialog
                    {
                        Title = "VOICEVOX.exe を選択してください",
                        Filter = "実行ファイル (*.exe)|*.exe",
                        CheckFileExists = true,
                        InitialDirectory = Path.GetDirectoryName(path)
                    };

                    if (ofd.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show("VOICEVOX の初期化をキャンセルしました。", "キャンセル", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }

                    // 新しいパスを保存
                    GlobalConfig.VoiceVoxPath = ofd.FileName;
                    GlobalConfig.Save(); // 必要に応じて保存
                    retryCount++;
                    continue;
                }

                // 起動試行
                _voiceVoxClient = new VoiceVoxClient();
                _voiceVoxClient.StartVoiceVoxAsync().Wait();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"VOICEVOX の起動に失敗しました。\n\n{ex.Message}",
                    "起動エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                retryCount++;
            }
        }

        MessageBox.Show(
            "VOICEVOX の起動に複数回失敗しました。\nアプリケーションを終了します。",
            "致命的なエラー",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        return false;
    }

    // タイムアウト付きのキー入力待機メソッド
    private static bool WaitForKeyPress(TimeSpan timeout)
    {
        var task = Task.Run(() =>
        {
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
            }
        });

        return Task.WaitAny(new[] { task }, timeout) == 0;
    }

    // コンソールアプリケーションのメインスレッド
    private static async void RunConsoleApp()
    {
        try
        {
            // テレメトリーの初期化
            InitializeTelemetry();

            // 音声認識のウォームアップ
            await WarmupSpeechRecognition();

            // 起動メッセージ
            await _voiceVoxClient!.SpeakAsync(
                "フェリー乗船サポートツールを起動しました。");

            // Ctrl+C処理
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; _isRunning = false; };

            // メインループ - ホットキー監視
            bool wasKeyPressed = false;
            while (_isRunning)
            {
                bool isKeyPressed = (GetAsyncKeyState(
                    GlobalConfig.Application.HotKeyVirtualKeyCode) & 0x8000) != 0;

                if (isKeyPressed && !wasKeyPressed)
                {
                    bool shouldContinue = await HandleVoiceInput();
                    if (!shouldContinue) break;
                }
                wasKeyPressed = isKeyPressed;
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            LogError("コンソールアプリケーション実行エラー", ex);
        }
        finally
        {
            try
            {
                if (_voiceVoxClient != null)
                {
                    await _voiceVoxClient.SpeakAsync("システムを終了します");
                    await Task.Delay(1000);
                }
            }
            catch { }

            // メインフォームの終了
            if (_mainForm != null && !_mainForm.IsDisposed && _mainForm.InvokeRequired)
                _mainForm.Invoke(new Action(() => Application.Exit()));
        }
    }

    // テレメトリーの初期化
    private static void InitializeTelemetry()
    {
        _telemetry = new SCSSdkTelemetry();
        _telemetry.Data += OnTelemetryData;
        _telemetry.resume();
    }

    // 音声認識ウォームアップ
    private static async Task WarmupSpeechRecognition()
    {
        if (GlobalConfig.Application.DebugMode)
            LogInfo("音声認識システムのウォームアップを開始します...");

        try
        {
            await Task.WhenAll(_speechRecognizer.RecognizeAsync(), Task.Delay(100));
        }
        catch { }

        if (GlobalConfig.Application.DebugMode)
            LogInfo("音声認識システムの準備が完了しました");
    }

    // テレメトリーデータ受信時の処理
    private static void OnTelemetryData(SCSTelemetry data, bool newTimestamp)
    {
        if (!newTimestamp) return;

        // テレメトリー受信フラグを立てる
        _telemetryReceived = true;

        try
        {
            // 積荷状態の変化があった場合のみ処理
            bool currentLoaded = data.JobValues.CargoLoaded;
            if (currentLoaded != _lastCargoLoaded)
            {
                if (currentLoaded)
                {
                    if (GlobalConfig.Application.DebugMode)
                        LogInfo($"Job started: {data.JobValues.CitySource} → {data.JobValues.CityDestination}");
                }
                else
                {
                    if (GlobalConfig.Application.DebugMode)
                        LogInfo("Job ended");
                }
                _lastCargoLoaded = currentLoaded;
            }

            // ジョブ情報の処理
            if (data.JobValues.CargoLoaded)
            {
                _jobActive = true;
                string currentSource = data.JobValues.CitySource;
                string currentDestination = data.JobValues.CityDestination;

                if (currentSource != _lastCitySource || currentDestination != _lastCityDestination)
                {
                    DisplayJobInfo(data.JobValues);
                    _lastCitySource = currentSource;
                    _lastCityDestination = currentDestination;
                }
            }
            else if (_jobActive)
            {
                // キャンセル／完了時にクリアのみ行う
                _jobActive = false;
                _lastCitySource = null;
                _lastCityDestination = null;
            }
        }
        catch (Exception ex)
        {
            LogError("テレメトリーデータ処理エラー", ex);
        }
    }

    // ジョブ情報の表示
    private static void DisplayJobInfo(SCSTelemetry.Job jobValues)
    {
        if (!GlobalConfig.Application.DebugMode) return;

        LogInfo($"Job Information");
        LogInfo($"User: {GlobalConfig.Application.CurrentUser}");
        LogInfo($"出発地: {jobValues.CitySource}");
        LogInfo($"到着地: {jobValues.CityDestination}");
        LogInfo("==================================");
    }

    // 音声入力処理
    private static async Task<bool> HandleVoiceInput()
    {
        if (_voiceVoxClient == null) return false;

        try
        {
            string? recognizedText = null;
            const int maxRetries = 2;
            int currentRetry = 0;

            while (string.IsNullOrEmpty(recognizedText) && currentRetry < maxRetries)
            {
                if (currentRetry > 0)
                {
                    await _voiceVoxClient.SpeakAsync("もう一度お願いします");
                }

                // 「どうぞ」と同時に音声認識を開始（並列実行）
                var speakTask = _voiceVoxClient.SpeakAsync("どうぞ");
                var recognizeTask = _speechRecognizer.RecognizeAsync();

                await speakTask; // 「どうぞ」の発話が終わるまで待機
                recognizedText = await recognizeTask; // 認識結果を取得

                if (GlobalConfig.Application.DebugMode)
                    LogInfo($"音声認識完了 (試行: {currentRetry + 1}/{maxRetries})");

                currentRetry++;
            }

            if (string.IsNullOrEmpty(recognizedText))
            {
                await _voiceVoxClient.SpeakAsync("申し訳ありません。もう一度キーを押して話しかけてください");
                return true;
            }

            if (GlobalConfig.Application.DebugMode)
                LogInfo($"認識されたテキスト: {recognizedText}");

            recognizedText = recognizedText.ToLower();

            // 終了コマンド
            if (recognizedText.Contains("終了") || recognizedText.Contains("終わり") || recognizedText.Contains("おわり"))
            {
                if (GlobalConfig.Application.DebugMode)
                    LogInfo("終了コマンドを受信しました");
                _isRunning = false;
                return false;
            }

            bool isAskingAboutDestination = recognizedText.Contains("どこ") || recognizedText.Contains("どっち") ||
                                            recognizedText.Contains("フェリー") || recognizedText.Contains("方面");

            bool isAskingAboutDirection = recognizedText.Contains("行き") || recognizedText.Contains("いき") ||
                                          recognizedText.Contains("行く") || recognizedText.Contains("いく") ||
                                          recognizedText.Contains("から");

            if (isAskingAboutDestination || isAskingAboutDirection)
            {
                return await HandleNavigationQuery();
            }
            else
            {
                await _voiceVoxClient.SpeakAsync("申し訳ありません。聞き取れませんでした。");
                return true; ;
            }
        }
        catch (Exception ex)
        {
            LogError("音声入力処理エラー", ex);
            await _voiceVoxClient.SpeakAsync("エラーが発生しました。もう一度お試しください");
            return true;
        }
    }

    // ナビゲーションクエリの処理
    private static async Task<bool> HandleNavigationQuery()
    {
        if (_voiceVoxClient == null) return false;

        // 1) テレメトリー未受信
        if (!_telemetryReceived)
        {
            await _voiceVoxClient.SpeakAsync("配送情報が取得できていません");
            return true;
        }
        // 2) ジョブ未受注/キャンセル後
        if (!_jobActive)
        {
            await _voiceVoxClient.SpeakAsync("現在お仕事を請け負っていません");
            return true;
        }
        // 3) 受注中だがDBにルートなし
        if (string.IsNullOrEmpty(_lastCitySource) || string.IsNullOrEmpty(_lastCityDestination))
        {
            await _voiceVoxClient.SpeakAsync("この区間にフェリーはありません");
            return true;
        }

        // 4) ルート取得
        var route = _dbHelper.GetRoute(_lastCitySource, _lastCityDestination);
        if (route != null)
        {
            string response = $"{route.BoardingPort}から{route.LandingPort}行きのフェリーに乗船してください";
            await _voiceVoxClient.SpeakAsync(response);
        }
        else
        {
            await _voiceVoxClient.SpeakAsync("この区間にフェリーはありません");
        }

        return true;
    }

    // リソースのクリーンアップ
    private static void CleanupResources()
    {
        _isRunning = false;

        try
        {
            // コンソールスレッドの終了を待機
            _consoleThread?.Join(1000);

            // リソースの解放
            _voiceVoxClient?.Dispose();
            _speechRecognizer?.Dispose();
            _telemetry?.Dispose();
            _dbHelper?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            // デバッグモード時の終了メッセージ
            if (GlobalConfig.Application.DebugMode)
            {
                LogInfo(GlobalConfig.Messages.ShutdownMessage);
                LogInfo("Press any key to exit...");

                // 3秒のタイムアウトで待機
                if (WaitForKeyPress(TimeSpan.FromSeconds(3)))
                {
                    Console.ReadKey(true);
                }
            }
        }
        catch (Exception ex)
        {
            // エラーは無視して終了処理を継続
            if (GlobalConfig.Application.DebugMode)
                LogError("クリーンアップ中にエラーが発生しました", ex);
        }
        finally
        {
            // 確実にコンソールウィンドウを閉じる
            var handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_HIDE);
            }
        }
    }

    // ログ関連のユーティリティメソッド
    private static void LogInfo(string message)
    {
        if (GlobalConfig.Application.DebugMode)
            Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] {message}");
    }

    private static void LogError(string message, Exception ex)
    {
        if (GlobalConfig.Application.DebugMode)
            Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] {message}: {ex.Message}");
    }
}