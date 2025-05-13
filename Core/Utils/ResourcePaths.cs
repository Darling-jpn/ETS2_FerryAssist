namespace ETS2_FerryAssist.Core.Utils
{
    /// <summary>
    /// アプリケーションで使用する各種リソースファイルおよびディレクトリのパスを定義するユーティリティクラス。
    /// </summary>
    public static class ResourcePaths
    {
        /// <summary>
        /// アプリケーションのベースディレクトリ（実行ファイルの配置先）。
        /// </summary>
        private static readonly string BaseDir = AppContext.BaseDirectory;

        /// <summary>
        /// ネイティブライブラリのパス管理。
        /// </summary>
        public static class Native
        {
            /// <summary>
            /// アプリケーションで必要なDLLファイルのリスト（例: VoiceVox動作用）。
            /// </summary>
            public static readonly string[] RequiredLibs = new[]
            {
                "libgcc_s_seh-1.dll",
                "libstdc++-6.dll"
            };

            /// <summary>
            /// 指定されたネイティブライブラリのフルパスを取得。
            /// </summary>
            public static string GetLibPath(string libName)
                => Path.Combine(BaseDir, libName);
        }

        /// <summary>
        /// リソースフォルダ内のファイルパスを管理。
        /// </summary>
        public static class Resources
        {
            private static readonly string ResourcesDir = Path.Combine(BaseDir, "Resources");

            /// <summary>
            /// アイコン関連リソース。
            /// </summary>
            public static class Icons
            {
                /// <summary>
                /// アプリケーションアイコンのパス。
                /// </summary>
                public static string AppIcon => Path.Combine(ResourcesDir, "Icons", "app.ico");
            }

            /// <summary>
            /// データベース関連リソース。
            /// </summary>
            public static class Database
            {
                /// <summary>
                /// フェリールート情報データベースのパス。
                /// </summary>
                public static string FerryRoutesDb => Path.Combine(ResourcesDir, "Database", "ferry_routes.db");
            }
        }

        /// <summary>
        /// 外部ライブラリやモデルなど、アプリケーション外部に依存するリソース。
        /// </summary>
        public static class External
        {
            private static readonly string ExternalDir = Path.Combine(BaseDir, "External");

            /// <summary>
            /// 音声モデル（例: Vosk）関連のパス。
            /// </summary>
            public static class VoiceModels
            {
                private static readonly string ModelsDir = Path.Combine(ExternalDir, "VoiceModels");

                /// <summary>
                /// Voskの日本語音声モデルのディレクトリパス。
                /// </summary>
                public static string VoskModelPath => Path.Combine(ModelsDir, "vosk", "vosk-model-small-ja-0.22");
            }

            /// <summary>
            /// SCSSdk（Euro Truck Simulator 2 用の Telemetry SDK）関連のパス。
            /// </summary>
            public static class SCSSdk
            {
                private static readonly string SdkDir = Path.Combine(ExternalDir, "SCSSdk");

                /// <summary>
                /// SCSSdkClient.dll のパス。
                /// </summary>
                public static string ClientDll => Path.Combine(SdkDir, "SCSSdkClient.dll");
            }
        }

        /// <summary>
        /// 必要なディレクトリ構造を作成する。
        /// 存在しないディレクトリがあれば作成する。
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            var directories = new[]
            {
                Path.Combine(BaseDir, "Resources", "Icons"),
                Path.Combine(BaseDir, "Resources", "Database"),
                Path.Combine(BaseDir, "External", "VoiceModels", "vosk"),
                Path.Combine(BaseDir, "External", "SCSSdk")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }
    }
}
