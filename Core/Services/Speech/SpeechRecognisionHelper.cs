using NAudio.Wave;
using Vosk;
using System.Text.Json;
using System.Text;
using ETS2_FerryAssist.Core.Configuration;

namespace ETS2_FerryAssist.Core.Services.Speech
{
    /// <summary>
    /// 音声認識処理を担当するヘルパークラス。
    /// Vosk（音声認識ライブラリ）と NAudio（音声入力制御）を統合し、非同期的に認識を行う。
    /// </summary>
    public class SpeechRecognitionHelper : IDisposable
    {
        // Vosk 音声認識器
        private readonly VoskRecognizer? _recognizer;

        // マイク入力イベント（NAudio）
        private readonly WaveInEvent? _waveIn;

        // 認識モデル（Vosk）
        private readonly Model? _model;

        // 録音中かどうかのフラグ
        private bool _isRecording;

        // 認識完了を通知する同期プリミティブ
        private readonly AutoResetEvent _recognitionComplete;

        // 最終的に認識されたテキスト
        private string _lastRecognizedText;

        // 現在の部分認識結果（リアルタイム途中表示用）
        private readonly StringBuilder _partialResult;

        // Dispose 済みフラグ
        private bool _disposed;

        /// <summary>
        /// 音声認識の初期化を行う。Vosk モデルとマイク入力を準備。
        /// </summary>
        public SpeechRecognitionHelper()
        {
            try
            {
                _recognitionComplete = new AutoResetEvent(false);
                _lastRecognizedText = string.Empty;
                _partialResult = new StringBuilder();

                // Vosk モデルディレクトリの存在確認
                if (!Directory.Exists(GlobalConfig.Vosk.ModelPath))
                {
                    throw new DirectoryNotFoundException($"Voskモデルが見つかりません: {GlobalConfig.Vosk.ModelPath}");
                }

                // モデルと認識器の初期化
                _model = new Model(GlobalConfig.Vosk.ModelPath);
                _recognizer = new VoskRecognizer(_model, 16000.0f);
                _recognizer.SetMaxAlternatives(0); // 最良の1件のみ
                _recognizer.SetWords(true); // 認識単語ごとの詳細を有効化

                // マイク入力設定（16kHz モノラル、50ms バッファ）
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 1),
                    BufferMilliseconds = 50
                };
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;

                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine("音声認識システムを初期化しました（Vosk）");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"音声認識の初期化に失敗しました: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 音声データが取得されるたびに呼び出される。認識処理を実行。
        /// </summary>
        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_isRecording && _recognizer != null)
            {
                // 完全な文が認識されたとき
                if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    var result = _recognizer.Result();
                    ProcessResult(result);
                }
                else
                {
                    // 認識中の部分的な結果
                    var partial = _recognizer.PartialResult();
                    ProcessPartial(partial);
                }
            }
        }

        /// <summary>
        /// 完全な認識結果を JSON から抽出して処理。
        /// </summary>
        private void ProcessResult(string result)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(result);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("text", out JsonElement textElement))
                {
                    string text = textElement.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _lastRecognizedText = text;
                        if (GlobalConfig.Application.DebugMode)
                        {
                            Console.WriteLine($"\n[デバッグ] 認識結果: {text}");
                        }
                        _recognitionComplete.Set(); // 待機解除
                    }
                }
            }
            catch (JsonException ex)
            {
                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine($"[デバッグ] JSON解析エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 認識途中の部分的な結果を処理し、画面に表示（デバッグ用）。
        /// </summary>
        private void ProcessPartial(string partial)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(partial);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("partial", out JsonElement partialElement))
                {
                    string text = partialElement.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _partialResult.Clear();
                        _partialResult.Append(text);
                        if (GlobalConfig.Application.DebugMode)
                        {
                            Console.Write($"\r[デバッグ] 認識中: {text}");
                        }
                    }
                }
            }
            catch (JsonException) { }
        }

        /// <summary>
        /// 録音停止イベント時に、最終結果を処理。
        /// </summary>
        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (_recognizer != null)
            {
                var finalResult = _recognizer.FinalResult();
                ProcessResult(finalResult);
            }
            _recognitionComplete.Set(); // 非同期の待機を解除
        }

        /// <summary>
        /// 音声認識処理を非同期で実行し、3秒間の入力を待つ。
        /// </summary>
        public async Task<string> RecognizeAsync()
        {
            try
            {
                _lastRecognizedText = string.Empty;
                _partialResult.Clear();
                _recognizer?.Reset();
                _recognitionComplete.Reset();

                _isRecording = true;
                _waveIn?.StartRecording();

                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine("\n[デバッグ] 録音開始");
                }

                await Task.Delay(3000); // 録音時間

                _isRecording = false;
                _waveIn?.StopRecording();

                // 結果が出るまで待機（録音停止イベントで解除）
                await Task.Run(() => _recognitionComplete.WaitOne());

                return _lastRecognizedText;
            }
            catch (Exception ex)
            {
                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine($"[エラー] 音声認識エラー: {ex.Message}");
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// IDisposableパターンによるリソース解放処理。
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _waveIn?.Dispose();
                    _recognizer?.Dispose();
                    _model?.Dispose();
                    _recognitionComplete.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 外部からのDispose呼び出し用。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
