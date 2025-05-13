using System.Diagnostics;
using System.Net.Http.Headers;
using System.Media;
using ETS2_FerryAssist.Core.Configuration;

namespace ETS2_FerryAssist.Core.Services.VoiceVox
{
    /// <summary>
    /// VoiceVoxエンジンと通信し、音声合成を行うクライアントクラス。
    /// VoiceVox APIの起動・状態確認・音声合成・再生を行う。
    /// </summary>
    public class VoiceVoxClient : IDisposable
    {
        // HTTP通信クライアント（VoiceVoxサーバーと通信）
        private readonly HttpClient _httpClient;

        // VoiceVoxの実行プロセス
        private Process? _voiceVoxProcess;

        // Dispose済みフラグ
        private bool _disposed;

        // 同時発話制御のためのセマフォ
        private readonly SemaphoreSlim _speakSemaphore;

        // 合成試行の最大回数
        private const int MaxRetries = 3;

        /// <summary>
        /// クライアントの初期化。HTTPクライアントとセマフォを設定。
        /// </summary>
        public VoiceVoxClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(GlobalConfig.VoiceVox.Endpoint),
                Timeout = TimeSpan.FromSeconds(Constants.VoiceVox.TimeoutSeconds)
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _speakSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// デバッグ出力のヘルパー（色付きエラーメッセージ対応）。
        /// </summary>
        private void WriteDebugLog(string message, bool isError = false)
        {
            if (GlobalConfig.Application.DebugMode)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";

                if (isError)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(logMessage);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.WriteLine(logMessage);
                }
            }
        }

        /// <summary>
        /// VoiceVoxのプロセスを起動し、APIが利用可能になるまで待機。
        /// </summary>
        public async Task<bool> StartVoiceVoxAsync()
        {
            // 実行ファイルパスの妥当性確認
            if (!GlobalConfig.VoiceVox.IsValidPath)
            {
                WriteDebugLog(Constants.Messages.VOICEVOX_NOT_FOUND, true);
                throw new InvalidOperationException(Constants.Messages.VOICEVOX_NOT_FOUND);
            }

            try
            {
                // すでにAPIが応答しているか確認
                if (await IsApiAvailableAsync())
                {
                    WriteDebugLog("既存のVoiceVox APIに接続しました");
                    return true;
                }

                WriteDebugLog("VoiceVoxを起動しています...");

                // プロセス構成・起動
                _voiceVoxProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = GlobalConfig.VoiceVox.ExecutablePath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        ErrorDialog = false
                    }
                };

                if (!_voiceVoxProcess.Start())
                {
                    throw new Exception(Constants.Messages.VOICEVOX_STARTUP_ERROR);
                }

                // タイムアウト付きでAPIの利用可能を待機
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Constants.VoiceVox.TimeoutSeconds));
                while (!cts.Token.IsCancellationRequested)
                {
                    if (await IsApiAvailableAsync())
                    {
                        WriteDebugLog("VoiceVox APIの準備が完了しました");
                        return true;
                    }
                    await Task.Delay(1000, cts.Token);
                }

                throw new TimeoutException("VoiceVox APIの準備がタイムアウトしました");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"VoiceVoxの起動中にエラーが発生しました: {ex.Message}", true);
                _voiceVoxProcess?.Dispose();
                _voiceVoxProcess = null;
                throw;
            }
        }

        /// <summary>
        /// VoiceVox APIの可用性確認（GET /version）。
        /// </summary>
        private async Task<bool> IsApiAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("version");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"API可用性チェックエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定テキストをVoiceVoxで合成・再生する。
        /// 最大3回までリトライ可能。
        /// </summary>
        public async Task<bool> SpeakAsync(string text)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(VoiceVoxClient));
            }

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    await _speakSemaphore.WaitAsync(); // 排他制御
                    WriteDebugLog($"音声合成開始: {text} (試行 {retryCount + 1}/{MaxRetries})");

                    // 音声合成用クエリの取得
                    var queryResponse = await _httpClient.PostAsync(
                        $"audio_query?text={Uri.EscapeDataString(text)}&speaker={GlobalConfig.VoiceVox.SpeakerId}",
                        null);
                    queryResponse.EnsureSuccessStatusCode();

                    var query = await queryResponse.Content.ReadAsStringAsync();

                    // 音声合成リクエストの送信
                    var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");
                    var synthesisResponse = await _httpClient.PostAsync(
                        $"synthesis?speaker={GlobalConfig.VoiceVox.SpeakerId}",
                        content);
                    synthesisResponse.EnsureSuccessStatusCode();

                    // 音声バイナリをメモリに読み取り
                    await using var audioStream = await synthesisResponse.Content.ReadAsStreamAsync();
                    using var ms = new MemoryStream();
                    await audioStream.CopyToAsync(ms);
                    ms.Position = 0;

                    // メモリから同期再生（SoundPlayer）
                    using var player = new SoundPlayer(ms);
                    player.PlaySync();

                    WriteDebugLog("音声合成完了");
                    return true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    WriteDebugLog($"音声合成エラー: {ex.Message}", true);

                    if (retryCount < MaxRetries)
                    {
                        WriteDebugLog($"リトライを実行します ({retryCount}/{MaxRetries})");
                        await Task.Delay(1000); // リトライ前の待機
                    }
                }
                finally
                {
                    _speakSemaphore.Release(); // セマフォ解放
                }
            }

            WriteDebugLog($"音声合成が{MaxRetries}回失敗しました", true);
            return false;
        }

        /// <summary>
        /// リソースの解放処理。
        /// VoiceVoxプロセスの終了やHTTPクライアント破棄などを行う。
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WriteDebugLog("VoiceVoxClientの破棄を開始します");

                    _httpClient.Dispose();
                    _speakSemaphore.Dispose();

                    if (_voiceVoxProcess != null && !_voiceVoxProcess.HasExited)
                    {
                        try
                        {
                            _voiceVoxProcess.Kill(true); // 強制終了（子プロセスも含む）
                            _voiceVoxProcess.WaitForExit(3000); // 最大3秒待機
                            WriteDebugLog("VoiceVoxプロセスを終了しました");
                        }
                        catch (Exception ex)
                        {
                            WriteDebugLog($"VoiceVoxプロセス終了エラー: {ex.Message}", true);
                        }
                        finally
                        {
                            _voiceVoxProcess.Dispose();
                        }
                    }

                    WriteDebugLog("VoiceVoxClientの破棄が完了しました");
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// IDisposable実装：明示的なDispose呼び出し。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// ファイナライザによるリソース解放（明示的Disposeがされなかった場合）。
        /// </summary>
        ~VoiceVoxClient()
        {
            Dispose(false);
        }
    }
}
