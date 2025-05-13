namespace ETS2_FerryAssist.Core.Utils
{
    /// <summary>
    /// アプリケーションで使用するリソースの初期化および整合性確認を行うユーティリティクラス。
    /// </summary>
    public static class ResourceManager
    {
        /// <summary>
        /// リソースの初期化を実行する。
        /// ディレクトリの存在確認、リソースファイルの検証、必要に応じてコピー処理などを行う。
        /// </summary>
        public static void Initialize()
        {
            ConsoleHelper.WriteLog("リソースの初期化を開始...");

            try
            {
                // 必要なディレクトリ構造を確認または作成
                ResourcePaths.EnsureDirectoriesExist();

                // リソースファイルが揃っているか確認し、不足していればコピーや例外を発生
                ValidateOrCopyResources();

                ConsoleHelper.WriteLog("リソースの初期化が完了しました");
            }
            catch (Exception ex)
            {
                // エラーが発生した場合はログ出力後に例外を再スロー
                ConsoleHelper.WriteLog($"リソースの初期化中にエラーが発生: {ex.Message}", true);
                throw;
            }
        }

        /// <summary>
        /// 各種リソースファイルの検証を行い、不足している場合はコピーまたは例外を投げる。
        /// </summary>
        private static void ValidateOrCopyResources()
        {
            // フェリールート用データベースファイルの存在確認
            var dbPath = ResourcePaths.Resources.Database.FerryRoutesDb;
            if (!File.Exists(dbPath))
            {
                // アプリケーション実行ディレクトリ内の初期リソースからコピー
                var sourceDbPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Database", "ferry_routes.db");
                File.Copy(sourceDbPath, dbPath, true);
                ConsoleHelper.WriteLog("データベースファイルをコピーしました");
            }

            // 音声認識用の Vosk モデルディレクトリの存在確認
            var voskModelPath = ResourcePaths.External.VoiceModels.VoskModelPath;
            if (!Directory.Exists(voskModelPath))
            {
                throw new DirectoryNotFoundException($"Voskモデルが見つかりません: {voskModelPath}");
            }
            ConsoleHelper.WriteLog("Voskモデルの確認が完了しました");

            // SCSSdkClient.dll の存在確認（SCS Telemetry SDK 用）
            var sdkClientPath = ResourcePaths.External.SCSSdk.ClientDll;
            if (!File.Exists(sdkClientPath))
            {
                throw new FileNotFoundException($"SCSSdkClient.dll が見つかりません: {sdkClientPath}");
            }
            ConsoleHelper.WriteLog("SCSSdkClient.dll の確認が完了しました");
        }
    }
}
