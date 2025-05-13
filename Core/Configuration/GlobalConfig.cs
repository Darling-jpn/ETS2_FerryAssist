using System.Text.Json;

namespace ETS2_FerryAssist.Core.Configuration
{
    /// <summary>
    /// アプリケーション全体のグローバル設定を管理する静的クラス。
    /// 設定のロード・保存、各カテゴリの設定オブジェクトを保持する。
    /// </summary>
    public static class GlobalConfig
    {
        // 設定ファイルのパス（実行ファイルと同じディレクトリにある config.json）
        private static readonly string ConfigFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");

        // 実際の設定データを保持する内部オブジェクト
        private static ConfigData _config;

        // クラス初期化時に設定を読み込む
        static GlobalConfig()
        {
            LoadConfig();
        }

        /// <summary>
        /// VoiceVox 関連の設定をまとめたサブクラス。
        /// 実行パス、話者ID、タイムアウト時間などを保持。
        /// </summary>
        public static class VoiceVox
        {
            // VoiceVoxエンドポイント（固定値）
            public static readonly string Endpoint = "http://127.0.0.1:50021";

            // 使用する話者IDの設定（保存時にファイル更新）
            public static int SpeakerId
            {
                get => _config.SpeakerId;
                set
                {
                    _config.SpeakerId = value;
                    SaveConfig();
                }
            }

            // タイムアウト秒数（定数から取得）
            public static int TimeoutSeconds => Constants.VoiceVox.TimeoutSeconds;

            // VoiceVox の実行ファイルパスの取得／設定（保存時にファイル更新）
            public static string ExecutablePath
            {
                get => _config.VoiceVoxPath;
                set
                {
                    _config.VoiceVoxPath = value;
                    SaveConfig();
                }
            }

            // 実行ファイルパスが有効かどうかのチェック
            public static bool IsValidPath => !string.IsNullOrEmpty(ExecutablePath) &&
                                              File.Exists(ExecutablePath) &&
                                              Path.GetFileName(ExecutablePath).ToLower().Contains("voicevox");

            // パスが設定されているかの簡易チェック
            public static bool HasVoiceVoxPath => !string.IsNullOrEmpty(ExecutablePath);
        }

        /// <summary>
        /// アプリケーション設定（バージョン、ホットキー、デバッグモードなど）をまとめたサブクラス。
        /// </summary>
        public static class Application
        {
            public static readonly string Version = "1.0.0";
            public static readonly string ReleaseDate = "2025-05-07";
            public static readonly string CurrentDateTime = "2025-05-09 05:21:11"; // UTC
            public static readonly string CurrentUser = "Darling-Japan";

            // ホットキーのデフォルト仮想キーコード（^キー）
            public const int DEFAULT_HOTKEY_VIRTUAL_CODE = 0xDE;

            // デバッグモードの設定（保存時にファイル更新）
            public static bool DebugMode
            {
                get => _config.DebugMode;
                set
                {
                    _config.DebugMode = value;
                    SaveConfig();
                }
            }

            // ホットキーの仮想キーコードの設定（保存時にファイル更新）
            public static int HotKeyVirtualKeyCode
            {
                get => _config.HotKeyVirtualKeyCode;
                set
                {
                    _config.HotKeyVirtualKeyCode = value;
                    SaveConfig();
                }
            }

            // ホットキー名の設定（保存時にファイル更新）
            public static string HotKeyName
            {
                get => _config.HotKeyName;
                set
                {
                    _config.HotKeyName = value;
                    SaveConfig();
                }
            }
        }

        /// <summary>
        /// 音声認識エンジン「Vosk」用の設定をまとめたサブクラス。
        /// </summary>
        public static class Vosk
        {
            public static readonly string ModelPath = "models/vosk-model-small-ja-0.22";
        }

        /// <summary>
        /// 起動・終了時のメッセージを定義したサブクラス。
        /// </summary>
        public static class Messages
        {
            public static readonly string StartupMessage =
                $"ETS2/ATS フェリー案内システム Starting at {Application.CurrentDateTime} UTC\n" +
                $"Created by: {Application.CurrentUser}";

            public static readonly string ShutdownMessage =
                $"終了時刻: {Application.CurrentDateTime} UTC";
        }

        /// <summary>
        /// 設定ファイルを読み込む。存在しない場合はデフォルト設定を作成。
        /// </summary>
        private static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _config = JsonSerializer.Deserialize<ConfigData>(json) ?? CreateDefaultConfig();
                }
                else
                {
                    _config = CreateDefaultConfig();
                    SaveConfig(); // 初回作成時に保存
                }
            }
            catch
            {
                _config = CreateDefaultConfig(); // エラー時も安全に初期化
            }
        }

        /// <summary>
        /// デフォルト設定を作成する。
        /// </summary>
        private static ConfigData CreateDefaultConfig()
        {
            return new ConfigData
            {
                DebugMode = false,
                HotKeyVirtualKeyCode = Application.DEFAULT_HOTKEY_VIRTUAL_CODE,
                HotKeyName = "OemHat",
                VoiceVoxPath = "",
                SpeakerId = 0
            };
        }

        /// <summary>
        /// 設定をファイルに保存する。
        /// </summary>
        private static void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // 見やすく整形
                };

                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                // デバッグモード時のみエラー出力
                if (Application.DebugMode)
                {
                    Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 外部から明示的に設定を保存するためのAPI。
        /// </summary>
        public static void SaveAllSettings()
        {
            SaveConfig();
        }

        /// <summary>
        /// VoiceVoxPath へのアクセス用エイリアス（後方互換性のため）
        /// </summary>
        public static string VoiceVoxPath
        {
            get => VoiceVox.ExecutablePath;
            set => VoiceVox.ExecutablePath = value;
        }

        /// <summary>
        /// SaveAllSettings の短縮メソッド（エイリアス）
        /// </summary>
        public static void Save()
        {
            SaveAllSettings();
        }

        /// <summary>
        /// 実際の設定値を保持する内部クラス。JSON にシリアライズされる。
        /// </summary>
        private class ConfigData
        {
            public bool DebugMode { get; set; }
            public int HotKeyVirtualKeyCode { get; set; } = Application.DEFAULT_HOTKEY_VIRTUAL_CODE;
            public string HotKeyName { get; set; } = "OemHat";
            public string VoiceVoxPath { get; set; } = string.Empty;
            public int SpeakerId { get; set; } = 0;
        }
    }
}
