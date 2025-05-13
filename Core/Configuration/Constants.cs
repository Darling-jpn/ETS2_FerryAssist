namespace ETS2_FerryAssist.Core.Configuration
{
    /// <summary>
    /// アプリケーション全体で利用される定数を一元管理する静的クラス。
    /// 設定値・メッセージ・API関連など用途別にまとめて記述。
    /// </summary>
    public static class Constants
    {
        /// VOICEVOX関連の定数。
        public static class VoiceVox
        {
            /// 起動・API待機時のタイムアウト秒数。
            public const int TimeoutSeconds = 30;
            /// VOICEVOXのAPIエンドポイント（デフォルト）。
            public const string DefaultEndpoint = "http://127.0.0.1:50021";
        }
        
        /// 共通メッセージやエラーメッセージ等。
        public static class Messages
        {
            public const string VOICEVOX_NOT_FOUND = "VoiceVoxの実行ファイルが設定されていません。\n設定画面からVoiceVoxの実行ファイルを指定してください。";
            public const string VOICEVOX_STARTUP_ERROR = "VoiceVoxの起動に失敗しました。";
            public const string SYNTHESIS_ERROR = "音声合成に失敗しました。";
        }

        public static class Paths
        {
        /// 共通Paths等。
            public static readonly string BaseDirectory = AppContext.BaseDirectory;
            public static readonly string LogDirectory = Path.Combine(BaseDirectory, "logs");
            public static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "config");
            public static readonly string DatabaseFile = Path.Combine(BaseDirectory, "ferry_routes.db");
        }
    }
}