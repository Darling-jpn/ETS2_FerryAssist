using System.Text.Json;
using ETS2_FerryAssist.Core.Configuration;
using ETS2_FerryAssist.Core.Services.VoiceVox;

namespace ETS2_FerryAssist.Forms
{
    /// <summary>
    /// アプリケーション設定を変更するためのフォームクラス。
    /// VoiceVox 実行パス、話者設定、ホットキー、デバッグモードなどを設定可能。
    /// </summary>
    public partial class SettingsForm : Form
    {
        // フォーム上で使用するコントロールを宣言
        private CheckBox chkDebugMode;
        private TextBox txtHotKey;
        private TextBox txtVoiceVoxPath;
        private ComboBox cmbSpeakers;
        private Button btnBrowseVoiceVox;
        private Button btnOK;
        private Button btnCancel;
        private Button btnTestVoice;
        private bool isCapturing = false;

        // Win32 APIを使ってコンソールウィンドウの制御
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        // コンストラクタ
        public SettingsForm()
        {
            InitializeComponent();
            try
            {
                using var ms = new MemoryStream(Properties.Resources.app);
                Icon = new Icon(ms);
            }
            catch
            {
                Icon = SystemIcons.Application;
            }
        }
        // UI コントロールの初期化
        private void InitializeComponent()
        {
            // コントロールのインスタンス化
            chkDebugMode = new CheckBox();
            txtHotKey = new TextBox();
            txtVoiceVoxPath = new TextBox();
            cmbSpeakers = new ComboBox();
            btnBrowseVoiceVox = new Button();
            btnTestVoice = new Button();
            btnOK = new Button();
            btnCancel = new Button();

            SuspendLayout();

            // タイトルラベルの設定
            var lblTitle = new Label
            {
                AutoSize = true,
                Location = new Point(12, 12),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                Text = "ETS2 FerryAssist Settings"
            };

            // デバッグモードチェックボックスの設定
            chkDebugMode.AutoSize = true;
            chkDebugMode.Location = new Point(12, 40);
            chkDebugMode.Text = "デバッグモード";

            // ホットキーラベルとテキストボックスの設定
            var lblHotKey = new Label
            {
                AutoSize = true,
                Location = new Point(12, 70),
                Text = "ホットキー設定:"
            };
            txtHotKey.Location = new Point(12, 90);
            txtHotKey.Size = new Size(430, 27);
            txtHotKey.ReadOnly = true;
            txtHotKey.Click += TxtHotKey_Click;
            txtHotKey.KeyDown += TxtHotKey_KeyDown;
            txtHotKey.Leave += TxtHotKey_Leave;

            // VoiceVox 実行ファイルパスのラベル、テキストボックス、参照ボタンの設定
            var lblVoiceVox = new Label
            {
                AutoSize = true,
                Location = new Point(12, 130),
                Text = "VoiceVox 実行ファイルパス:"
            };
            txtVoiceVoxPath.Location = new Point(12, 150);
            txtVoiceVoxPath.Size = new Size(430, 27);
            txtVoiceVoxPath.Text = GlobalConfig.VoiceVox.ExecutablePath;
            btnBrowseVoiceVox.Location = new Point(450, 150);
            btnBrowseVoiceVox.Size = new Size(75, 27);
            btnBrowseVoiceVox.Text = "参照...";
            btnBrowseVoiceVox.Click += BtnBrowseVoiceVox_Click;

            // 話者選択のラベル、コンボボックス、テストボタンの設定
            var lblSpeaker = new Label
            {
                AutoSize = true,
                Location = new Point(12, 190),
                Text = "話者 (Speaker):"
            };
            cmbSpeakers.Location = new Point(12, 210);
            cmbSpeakers.Size = new Size(430, 27);
            cmbSpeakers.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpeakers.Items.Add(new ComboItem("取得中...", -1));// 初期値として「取得中」を表示
            cmbSpeakers.SelectedIndex = 0;

            btnTestVoice.Location = new Point(450, 210);
            btnTestVoice.Size = new Size(75, 27);
            btnTestVoice.Text = "テスト";
            btnTestVoice.Click += BtnTestVoice_Click;// テスト音声を再生する

            // OK とキャンセルボタンの設定
            btnOK.Location = new Point(280, 260);
            btnOK.Size = new Size(90, 30);
            btnOK.Text = "OK";
            btnOK.Click += BtnOK_Click;// 設定を保存
            btnCancel.Location = new Point(380, 260);
            btnCancel.Size = new Size(90, 30);
            btnCancel.Text = "キャンセル";
            btnCancel.DialogResult = DialogResult.Cancel;// キャンセルボタンが押されたときの動作

            // フォームの設定
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            ClientSize = new Size(550, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "設定";

            // コントロールをフォームに追加
            Controls.AddRange(new Control[]
            {
                lblTitle,
                chkDebugMode,
                lblHotKey, txtHotKey,
                lblVoiceVox, txtVoiceVoxPath, btnBrowseVoiceVox,
                lblSpeaker, cmbSpeakers, btnTestVoice,
                btnOK, btnCancel
            });

            Load += SettingsForm_Load; // フォームロード時に初期化処理
            ResumeLayout(false);
            PerformLayout();
        }
        // フォームロード時の設定
        private async void SettingsForm_Load(object sender, EventArgs e)
{
    // 初期値の設定
    chkDebugMode.Checked = GlobalConfig.Application.DebugMode;
    txtHotKey.Text = "現在のキー: " + GlobalConfig.Application.HotKeyName;
    txtVoiceVoxPath.Text = GlobalConfig.VoiceVox.ExecutablePath;

    // VoiceVox実行ファイルパスが設定されていれば、話者リストを取得）
    if (!string.IsNullOrEmpty(GlobalConfig.VoiceVox.ExecutablePath))
    {
        await UpdateSpeakerList(true);
    }
}
        // VoiceVoxクライアントのインスタンスを作成
        private VoiceVoxClient? _voiceVoxClient;

        // 話者リストを更新する非同期メソッド
        private async Task UpdateSpeakerList(bool isInitialSetup = false)
        {
            // 初期状態では「取得中...」を表示
            cmbSpeakers.Items.Clear();
            cmbSpeakers.Items.Add(new ComboItem("取得中...", -1));
            cmbSpeakers.SelectedIndex = 0;
            cmbSpeakers.Enabled = false;// 話者選択を無効化
            btnTestVoice.Enabled = false;// テストボタンも無効化

            try
            {
                if (_voiceVoxClient == null)
                {
                    _voiceVoxClient = new VoiceVoxClient();
                }

                // 初期設定時にVOICEVOXを起動
                if (isInitialSetup)
                {
                    await _voiceVoxClient.StartVoiceVoxAsync();
                    await Task.Delay(1000);// 起動待機
                }
                // APIを呼び出して話者リストを取得
                using var client = new HttpClient { BaseAddress = new Uri(GlobalConfig.VoiceVox.Endpoint) };
                client.Timeout = TimeSpan.FromSeconds(5); // タイムアウトを設定

                var resp = await client.GetAsync("/speakers");
                resp.EnsureSuccessStatusCode();
                var list = JsonSerializer.Deserialize<SpeakerInfo[]>(
                    await resp.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // コンボボックスに話者情報を追加
                cmbSpeakers.Items.Clear();
                foreach (var sp in list)
                {
                    foreach (var st in sp.Styles)
                    {
                        cmbSpeakers.Items.Add(new ComboItem(sp.Name + " (" + st.Name + ")", st.Id));
                    }
                }

                // 設定された話者IDがあれば選択
                var selected = cmbSpeakers.Items.Cast<ComboItem>()
                    .FirstOrDefault(x => x.Value == GlobalConfig.VoiceVox.SpeakerId);
                if (selected != null)
                    cmbSpeakers.SelectedItem = selected;
                else if (cmbSpeakers.Items.Count > 0)
                    cmbSpeakers.SelectedIndex = 0;

                // 話者選択とテストボタンを有効化
                cmbSpeakers.Enabled = true;
                btnTestVoice.Enabled = true;
            }
            catch (Exception ex)
            {
                // 取得失敗時のエラーメッセージ表示
                cmbSpeakers.Items.Clear();
                cmbSpeakers.Items.Add(new ComboItem("取得失敗", -1));
                cmbSpeakers.SelectedIndex = 0;
                cmbSpeakers.Enabled = false;
                btnTestVoice.Enabled = false;

                string errorMessage = isInitialSetup ?
                    $"VOICEVOXの起動または接続に失敗しました。\n実行ファイルのパスが正しいか確認してください。\n\nエラー詳細:\n{ex.Message}" :
                    $"話者一覧の取得に失敗しました。\nVOICEVOXが起動しているか確認してください。\n\nエラー詳細:\n{ex.Message}";

                MessageBox.Show(
                    errorMessage,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ホットキー設定クリック時の処理
        private void TxtHotKey_Click(object sender, EventArgs e)
        {
            isCapturing = true;
            txtHotKey.Text = "キーを入力してください...";
        }

        // ホットキーキー入力時の処理
        private void TxtHotKey_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isCapturing) return;
            e.SuppressKeyPress = true;
            GlobalConfig.Application.HotKeyVirtualKeyCode = e.KeyValue;
            GlobalConfig.Application.HotKeyName = e.KeyCode.ToString();
            txtHotKey.Text = "現在のキー: " + GlobalConfig.Application.HotKeyName;
            isCapturing = false;
        }

        // ホットキー設定を終了した際の処理
        private void TxtHotKey_Leave(object sender, EventArgs e)
        {
            if (!isCapturing) return;
            txtHotKey.Text = "現在のキー: " + GlobalConfig.Application.HotKeyName;
            isCapturing = false;
        }

        // VOICEVOXの実行ファイルパスを変更する処理
        private async void BtnBrowseVoiceVox_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "実行ファイル (*.exe)|*.exe|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtVoiceVoxPath.Text = dlg.FileName;
                GlobalConfig.VoiceVox.ExecutablePath = dlg.FileName;

                // パス変更後にVOICEVOXを起動して話者一覧を更新
                await UpdateSpeakerList(true);
            }
        }
        // フォームを閉じる前にVOICEVOXクライアントを破棄
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_voiceVoxClient != null)
            {
                _voiceVoxClient.Dispose();
                _voiceVoxClient = null;
            }
            base.OnFormClosing(e);
        }
        // テスト音声を再生する処理
        private async void BtnTestVoice_Click(object sender, EventArgs e)
        {
            if (_voiceVoxClient == null || cmbSpeakers.SelectedItem is not ComboItem selectedItem || selectedItem.Value < 0)
            {
                return;
            }

            btnTestVoice.Enabled = false;
            try
            {
                // テスト時に選択中の話者IDを使用
                int currentSpeakerId = selectedItem.Value;

                // VoiceVoxClientの話者IDを一時的に変更
                int originalSpeakerId = GlobalConfig.VoiceVox.SpeakerId;
                try
                {
                    GlobalConfig.VoiceVox.SpeakerId = currentSpeakerId;
                    await _voiceVoxClient.SpeakAsync("音声テストです");
                }
                finally
                {
                    // テスト後、元の話者IDに戻す（OKボタンを押すまでは設定を保存しない）
                    GlobalConfig.VoiceVox.SpeakerId = originalSpeakerId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"テスト音声の再生に失敗しました。\n\nエラー詳細:\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnTestVoice.Enabled = true;
            }
        }
        // コンボボックスの選択変更イベントを追加
        private void cmbSpeakers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSpeakers.SelectedItem is ComboItem selectedItem && selectedItem.Value >= 0)
            {
                // この時点では設定を保存せず、テスト再生用に一時的に保持するだけ
                _currentSelectedSpeakerId = selectedItem.Value;
            }
        }
        // OK ボタンクリック時の処理も修正
        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 入力検証
            if (string.IsNullOrEmpty(txtVoiceVoxPath.Text) || !File.Exists(txtVoiceVoxPath.Text))
            {
                MessageBox.Show(
                    "有効な VoiceVox 実行ファイルを選択してください。",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // 選択された話者IDを保存
            if (cmbSpeakers.SelectedItem is ComboItem ci && ci.Value >= 0)
            {
                GlobalConfig.VoiceVox.SpeakerId = ci.Value;
            }

            GlobalConfig.Application.DebugMode = chkDebugMode.Checked;
            GlobalConfig.VoiceVox.ExecutablePath = txtVoiceVoxPath.Text;
            GlobalConfig.SaveAllSettings();
            DialogResult = DialogResult.OK;
            Close();
        }
        public void DisableCancelButton()
        {
            if (btnCancel != null)
            {
                btnCancel.Enabled = false;
            }
        }
        // フォームのフィールドに選択中の話者IDを保持するための変数を追加
        private int _currentSelectedSpeakerId = -1;
        // Helper classes
        private class SpeakerInfo
        {
            public string Name { get; set; }
            public StyleInfo[] Styles { get; set; }
        }

        private class StyleInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class ComboItem
        {
            public string Text { get; }
            public int Value { get; }
            public ComboItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}