using ETS2_FerryAssist.Core.Configuration;
using System.Runtime.InteropServices;

namespace ETS2_FerryAssist.Core.Utils
{
    public static class ConsoleHelper
    {
        // Windows API: 出力CP（コードページ）を設定する
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        // Windows API: 現在のコンソールウィンドウのハンドルを取得
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        // Windows API: ウィンドウの表示状態を変更（表示／非表示）
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // 表示／非表示の定数
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static readonly object LockObj = new(); // 排他制御用ロックオブジェクト
        private static bool _isInitialized;             // 初期化済みかどうかのフラグ

        /// <summary>
        /// コンソールのエンコーディングを UTF-8 に設定（初回のみ実行）
        /// </summary>
        public static void InitializeConsole()
        {
            if (_isInitialized) return;

            lock (LockObj)
            {
                if (_isInitialized) return;

                SetConsoleOutputCP(65001);  // UTF-8 コードページを設定
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                _isInitialized = true;
            }
        }

        /// <summary>
        /// デバッグコンソールを表示し、情報ヘッダーを出力
        /// </summary>
        public static void ShowDebugConsole()
        {
            var consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_SHOW); // コンソールを表示
                Console.Clear();
                Console.Title = "ETS2フェリー案内システム - デバッグコンソール";

                // デバッグ情報の出力
                Console.WriteLine($"Current Date and Time (UTC - YYYY-MM-DD HH:MM:SS formatted): {GlobalConfig.Application.CurrentDateTime}");
                Console.WriteLine($"Current User's Login: {GlobalConfig.Application.CurrentUser}");
                Console.WriteLine("デバッグモードが有効になりました。");
                Console.WriteLine("----------------------------------------");
            }
        }

        /// <summary>
        /// コンソールウィンドウを非表示にする
        /// </summary>
        public static void HideConsole()
        {
            var consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE); // コンソールを隠す
            }
        }

        /// <summary>
        /// タイムスタンプ付きでログメッセージを出力（エラー時は赤色で表示）
        /// </summary>
        /// <param name="message">出力するメッセージ</param>
        /// <param name="isError">エラーメッセージかどうか</param>
        public static void WriteLog(string message, bool isError = false)
        {
            lock (LockObj)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var formattedMessage = $"[{timestamp}] {message}";

                if (isError)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(formattedMessage);
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    Console.WriteLine(formattedMessage);
                }
            }
        }
    }
}
