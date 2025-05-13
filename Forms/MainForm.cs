using System.ComponentModel;
using ETS2_FerryAssist.Core.Configuration;

namespace ETS2_FerryAssist.Forms
{
    /// <summary>
    /// アプリケーションのメインフォーム（非表示にしてシステムトレイで動作）。
    /// 設定画面の起点や、トレイメニュー操作を担当。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Icon _appIcon;
        private readonly IContainer components;

        /// <summary>
        /// MainForm の初期化。UIは非表示とし、システムトレイに常駐させる。
        /// </summary>
        public MainForm()
        {
            components = new Container();
            InitializeComponent();

            try
            {
                // アプリケーションアイコンの読み込み（リソースから）
                try
                {
                    using (MemoryStream ms = new MemoryStream(Properties.Resources.app))
                    {
                        _appIcon = new Icon(ms);
                    }
                    Icon = _appIcon;
                }
                catch (Exception ex)
                {
                    // 読み込み失敗時はデフォルトアイコンを使用
                    if (GlobalConfig.Application.DebugMode)
                    {
                        Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] Failed to load icon from resources: {ex.Message}");
                    }
                    _appIcon = SystemIcons.Application;
                    Icon = _appIcon;
                }

                // フォーム自体は完全に非表示に設定
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                Opacity = 0;
                Size = new Size(0, 0);

                // システムトレイアイコンの構成
                _notifyIcon = new NotifyIcon(components)
                {
                    Icon = _appIcon,
                    Text = "ETS2 FerryAssist",
                    Visible = true
                };

                // トレイメニュー構成
                var contextMenu = new ContextMenuStrip(components);

                var infoItem = new ToolStripMenuItem($"Started: {GlobalConfig.Application.CurrentDateTime}") { Enabled = false };
                var userItem = new ToolStripMenuItem($"User: {GlobalConfig.Application.CurrentUser}") { Enabled = false };
                var settingsItem = new ToolStripMenuItem("設定", _appIcon.ToBitmap(), (s, e) => OpenSettings());
                var exitItem = new ToolStripMenuItem("終了", _appIcon.ToBitmap(), (s, e) => ConfirmAndExit());

                contextMenu.Items.AddRange(new ToolStripItem[]
                {
                    infoItem,
                    userItem,
                    new ToolStripSeparator(),
                    settingsItem,
                    new ToolStripSeparator(),
                    exitItem
                });

                _notifyIcon.ContextMenuStrip = contextMenu;

                // ダブルクリックで設定画面を表示
                _notifyIcon.DoubleClick += (s, e) => OpenSettings();

                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] MainForm initialized successfully");
                }
            }
            catch (Exception ex)
            {
                // 重大な初期化失敗時はメッセージ表示
                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] MainForm initialization error: {ex.Message}");
                }
                MessageBox.Show(
                    $"アプリケーションの初期化中にエラーが発生しました。\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// コンポーネント初期化処理（フォーム基本設定）
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(120, 0); // 実質表示しない
            Name = "MainForm";
            Text = "ETS2 PJ フェリー乗船サポートツール";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;

            ResumeLayout(false);
        }

        /// <summary>
        /// フォームロード時に設定画面を開く（非表示UIでも動作確認のため）
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (GlobalConfig.Application.DebugMode)
            {
                Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] MainForm loaded");
            }

            BeginInvoke(new Action(() => OpenSettings()));
        }

        /// <summary>
        /// 通常のウィンドウクローズ操作をキャンセルし、バックグラウンド常駐を維持。
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
        }

        /// <summary>
        /// ユーザーに確認してアプリケーションを終了。
        /// </summary>
        private void ConfirmAndExit()
        {
            if (MessageBox.Show(
                "アプリケーションを終了しますか？",
                "確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// 設定フォームを表示（モーダル）
        /// </summary>
        private void OpenSettings()
        {
            try
            {
                using var settingsForm = new SettingsForm();
                settingsForm.ShowDialog();

                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] Settings dialog opened");
                }
            }
            catch (Exception ex)
            {
                if (GlobalConfig.Application.DebugMode)
                {
                    Console.WriteLine($"[{GlobalConfig.Application.CurrentDateTime}] Settings dialog error: {ex.Message}");
                }
                MessageBox.Show(
                    $"設定画面の表示中にエラーが発生しました。\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 明示的にフォームを非表示にするためにオーバーライド。
        /// </summary>
        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
                value = false;
            }
            base.SetVisibleCore(false);
        }

        /// <summary>
        /// 使用リソースの解放処理（トレイアイコン、アイコン、コンポーネント）
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    if (_notifyIcon.ContextMenuStrip != null)
                    {
                        foreach (ToolStripItem item in _notifyIcon.ContextMenuStrip.Items)
                        {
                            item.Image?.Dispose();
                        }
                        _notifyIcon.ContextMenuStrip.Dispose();
                    }
                    _notifyIcon.Dispose();
                }

                _appIcon?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
