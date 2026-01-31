using System.Reflection;
using AlignTaiko.Core;
using AlignTaiko.Gui.Properties;
using AlignTaiko.Gui.Services;
using System.Globalization;

namespace AlignTaiko.Gui
{
    public sealed partial class MainForm : Form
    {
        private bool _uiInitializing;

        // UI
        private readonly RadioButton rbSingle = new();
        private readonly RadioButton rbBatch = new();

        private readonly TextBox txtInput = new();

        private readonly Button btnRun = new();

        private readonly ComboBox cmbLang = new();

        // Localization targets (were local variables)
        private readonly Label lblOutput = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

        private readonly DashedDropPanel pnlDrop = new();
        private readonly Label lblDropHint = new();

        private readonly CheckBox chkBackup = new();
        private readonly Button btnOpenBackup = new();

        private readonly BackupService _backup = new();
        private readonly BatchPreviewService _batchPreview = new();

        private string? _inputFullPath;

        private readonly ListBox lstBatchTargets = new();

        public MainForm()
        {
            _uiInitializing = true;

            var asm = Assembly.GetExecutingAssembly();
            var resName = "AlignTaiko.Gui.Assets.Icon.app.ico";
            using var s = asm.GetManifestResourceStream(resName);
            if (s != null) this.Icon = new Icon(s);

            // 前回の言語設定を先に反映（Resources を正しい言語で引くため）
            var cultureName = AppConfig.LoadCultureNameOrDefault();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);

            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(400, 320);
            MaximumSize = new Size(int.MaxValue, 320);
            StartPosition = FormStartPosition.CenterScreen;

            AllowDrop = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(12),
                AutoSize = false,
            };

            // 左：内容 / 右：Language
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // top
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // input
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 95)); // drop
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // backup checkbox row
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // bottom (buttons)

            Controls.Add(root);

            // UI生成が終わった直後に一度だけ
            UiTheme.Apply(this);

            // もし変数名がこの通りなら（あなたの実装に合わせて）
            UiTheme.StyleDropArea(pnlDrop, lblDropHint);
            UiTheme.SetPrimaryButton(btnRun);   // Runをアクセントに
            UiTheme.StyleButton(btnOpenBackup); // 明示してもOK（なくても再帰で当たる）

            // --- 1行目：Single/Batch（左） + Language（右） ---
            var pnlTopLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };

            rbSingle.AutoSize = true;
            rbBatch.AutoSize = true;
            rbSingle.Checked = true;

            rbSingle.CheckedChanged += (_, _) =>
            {
                UpdateUiState();
                RefreshBatchPreview();
            };

            rbBatch.CheckedChanged += (_, _) =>
            {
                UpdateUiState();
                RefreshBatchPreview();
            };

            pnlTopLeft.Controls.Add(rbSingle);
            pnlTopLeft.Controls.Add(rbBatch);

            cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLang.Dock = DockStyle.Fill;
            cmbLang.SelectedIndexChanged += (_, _) => OnLanguageChanged();

            root.Controls.Add(pnlTopLeft, 0, 0);
            root.Controls.Add(cmbLang, 1, 0);

            // --- 2行目：Input path textbox ---
            txtInput.ReadOnly = true;
            txtInput.BackColor = UiTheme.BgReadOnly;
            txtInput.ForeColor = UiTheme.TextMuted;
            txtInput.BorderStyle = BorderStyle.FixedSingle;

            txtInput.Dock = DockStyle.Fill;
            txtInput.Margin = new Padding(0, 6, 0, 0);

            root.Controls.Add(txtInput, 0, 1);
            root.SetColumnSpan(txtInput, 2);

            txtInput.ReadOnly = true;
            txtInput.TabStop = false;     // Tabでフォーカスしない

            // --- 3行目：大きい Drag & Drop 枠 ---
            pnlDrop.Dock = DockStyle.Fill;
            pnlDrop.Margin = new Padding(0, 10, 0, 0);
            pnlDrop.AllowDrop = true;

            // ★ BorderStyle は使わない（点線枠は自前描画）
            pnlDrop.BorderStyle = BorderStyle.None;

            // ★ DashedDropPanel として色を設定（pnlDrop が DashedDropPanel で作られている前提）
            if (pnlDrop is DashedDropPanel drop)
            {
                drop.BorderColor = UiTheme.Border;
                drop.HoverBorderColor = UiTheme.AccentSoft;
                drop.BorderWidth = 1.1f;
                drop.DashLength = 1.6f;
                drop.DashGap = 1.4f;
            }

            lblDropHint.Dock = DockStyle.Fill;
            lblDropHint.TextAlign = ContentAlignment.MiddleCenter;
            lblDropHint.AutoEllipsis = true;
            lblDropHint.ForeColor = UiTheme.TextMuted;

            pnlDrop.Controls.Clear();
            pnlDrop.Controls.Add(lblDropHint);

            // DragEnter / DragLeave / DragDrop で「ドラッグ中」も強調したい場合：
            // （Hoverとは別に、ドラッグが入ってきたときだけさらに明るくする）
            Color? savedHover = null;

            pnlDrop.DragEnter += (_, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;

                    if (pnlDrop is DashedDropPanel d)
                        d.IsDragActive = true;   // ★Drag中ON
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };

            pnlDrop.DragLeave += (_, __) =>
            {
                if (pnlDrop is DashedDropPanel d)
                    d.IsDragActive = false;      // ★Drag中OFF
            };

            pnlDrop.DragDrop += (_, e) =>
            {
                if (pnlDrop is DashedDropPanel d)
                    d.IsDragActive = false;      // ★Drop後OFF

                if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (paths.Length == 0) return;
                ApplyDroppedPath(paths[0]);
            };

            pnlDrop.Click += (_, _) => BrowseInput();
            lblDropHint.Click += (_, _) => BrowseInput();


            // --- 4行目：Batch 対象一覧（Batchのときだけ表示） ---
            lstBatchTargets.Dock = DockStyle.Fill;
            lstBatchTargets.IntegralHeight = false;
            lstBatchTargets.Visible = false;

            root.Controls.Add(pnlDrop, 0, 2);
            root.SetColumnSpan(pnlDrop, 2);

            // --- 4行目：Backup checkbox + Open backup + Run ---
            var pnlBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0),
                Padding = new Padding(0),
            };

            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // open backup
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // run

            chkBackup.AutoSize = true;

            chkBackup.CheckedChanged += (_, _) =>
            {
                if (_uiInitializing) return; // 初期値反映時の発火は無視
                AppConfig.SaveBackupEnabled(chkBackup.Checked);
            };

            chkBackup.Margin = new Padding(0, 15, 0, 0); // 上に少し余白（好みで0でもOK）
            chkBackup.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            root.Controls.Add(chkBackup, 0, 3);
            root.SetColumnSpan(chkBackup, 2);

            btnOpenBackup.Dock = DockStyle.Fill;
            btnOpenBackup.Enabled = false;
            btnOpenBackup.Click += (_, _) => _backup.OpenLastFolder();

            btnRun.Font = new Font(btnRun.Font, FontStyle.Bold);
            btnRun.Dock = DockStyle.Fill;
            btnRun.Click += (_, _) => Run();

            pnlBottom.Controls.Add(btnOpenBackup, 0, 0);
            pnlBottom.Controls.Add(btnRun, 1, 0);

            root.Controls.Add(pnlBottom, 0, 4);
            root.SetColumnSpan(pnlBottom, 2);

            // ローカライズ
            InitLanguageCombo();
            ApplyLocalization();
            UpdateUiState();
            RefreshBatchPreview();

            // 前回の設定をUIに反映（初期化中なので保存は発火しない）
            chkBackup.Checked = AppConfig.LoadBackupEnabledOrDefault();

            _uiInitializing = false;
        }

        private void InitLanguageCombo()
        {
            // 表示名は固定文字列でもOK。内部値は cultureName
            cmbLang.Items.Clear();
            cmbLang.Items.Add(new LangItem("English", "en-US"));
            cmbLang.Items.Add(new LangItem("日本語", "ja-JP"));

            var cur = Thread.CurrentThread.CurrentUICulture.Name;
            var idx = 0;
            for (int i = 0; i < cmbLang.Items.Count; i++)
            {
                if (((LangItem)cmbLang.Items[i]!).CultureName.Equals(cur, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
            cmbLang.SelectedIndex = idx;
        }

        private void ApplyLocalization()
        {
            Text = Resources.AppTitle;

            rbSingle.Text = Resources.Single;
            rbBatch.Text = Resources.Batch;

            btnRun.Text = Resources.Run;
            chkBackup.Text = Resources.BackupEnabled;
            btnOpenBackup.Text = Resources.OpenBackupFolder;

            lblDropHint.Text = Resources.DropHint;
        }

        private void UpdateUiState()
        {
            txtInput.PlaceholderText = rbSingle.Checked
                   ? Resources.InputHintSingle  // 例: "Input file path..."
                   : Resources.InputHintBatch;  // 例: "Input folder path..."
        }

        private void OnLanguageChanged()
        {
            if (_uiInitializing) return; // ★初期化中に SelectedIndexChanged が発火するのを無視
            if (cmbLang.SelectedItem is not LangItem item) return;

            // ★すでに同じ言語なら何もしない（無限ループ予防）
            var current = Thread.CurrentThread.CurrentUICulture.Name;
            if (string.Equals(current, item.CultureName, StringComparison.OrdinalIgnoreCase))
                return;

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(item.CultureName);
            AppConfig.SaveCulture(item.CultureName);

            // フォームは作り直さず、表示文字だけ更新する
            ApplyLocalization();
            UpdateUiState();
        }

        private void BrowseInput()
        {
            // Single/Batch どちらでも .osu を選ぶ
            using var ofd = new OpenFileDialog
            {
                Filter = "osu files (*.osu)|*.osu|All files (*.*)|*.*",
                Title = rbSingle.Checked ? Resources.SelectInputFile : Resources.SelectBatchSeedFile
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                _inputFullPath = ofd.FileName;
                txtInput.Text = Path.GetFileName(ofd.FileName);
                RefreshBatchPreview();
                UpdateUiState();
            }
        }

        private void ApplyDroppedPath(string p)
        {
            _inputFullPath = p;

            if (File.Exists(p))
            {
                rbSingle.Checked = true;
                txtInput.Text = Path.GetFileName(p);   // ★表示はファイル名のみ
            }
            else if (Directory.Exists(p))
            {
                rbBatch.Checked = true;
                txtInput.Text = Path.GetFileName(
                    p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                ); // ★フォルダ名のみ
            }

            UpdateUiState();
        }

        private void RefreshBatchPreview()
        {
            lstBatchTargets.Items.Clear();

            if (!rbBatch.Checked)
            {
                lstBatchTargets.Visible = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(_inputFullPath))
            {
                lstBatchTargets.Visible = false;
                return;
            }

            var diffs = _batchPreview.GetDiffNames(_inputFullPath);
            if (diffs.Count == 0)
            {
                lstBatchTargets.Visible = false;
                return;
            }

            foreach (var d in diffs)
                lstBatchTargets.Items.Add(d);

            lstBatchTargets.Visible = true;
        }


        private void Run()
        {
            var input = _inputFullPath;

            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show(this, Resources.ErrorInputNotFound, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var opt = new AlignOptions(256, 192);

            try
            {
                if (rbSingle.Checked)
                {
                    var confirm = MessageBox.Show(
                        this,
                        Resources.ConfirmOverwrite,
                        Text,
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.OK) return;

                    if (!File.Exists(input))
                    {
                        MessageBox.Show(this, Resources.ErrorInputNotFound, Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (chkBackup.Checked)
                    {
                        var baseDir = Path.GetDirectoryName(input) ?? "";
                        var runFolder = _backup.CreateBackupRunFolder(baseDir);
                        _backup.BackupFileTo(input, runFolder);

                        btnOpenBackup.Enabled = _backup.CanOpenLastFolder();
                    }

                    var r = OverwriteHelper.AlignFileOverwriteSafe(input, opt);
                    if (!r.Success)
                    {
                        MessageBox.Show(this, $"{Resources.Failed}: {r.Error}", Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    MessageBox.Show(this,
                        $"{Resources.DoneSingle}: {r.ChangedObjects}",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Batch でも「選んだ .osu と同じ階層のみ」を処理する
                    // _inputFullPath は「選択された .osu のフルパス」
                    if (string.IsNullOrWhiteSpace(_inputFullPath) || !File.Exists(_inputFullPath))
                        return;

                    var seedOsu = _inputFullPath;
                    var dir = Path.GetDirectoryName(seedOsu);
                    if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                        return;

                    var diffs = _batchPreview.GetDiffNames(seedOsu);
                    if (diffs.Count == 0)
                    {
                        MessageBox.Show(this, Resources.ErrorInputNotFound, Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 表示整形：すでに "[Oni]" 形式ならそのまま、違うなら [] を付ける
                    static string Bracket(string s)
                    {
                        s = s.Trim();
                        return (s.StartsWith("[") && s.EndsWith("]")) ? s : $"[{s}]";
                    }

                    var msg = Resources.ConfirmBatchPrefix
                              + "\n\n"
                              + string.Join("\n", diffs.Select(Bracket));

                    var confirmBatch = MessageBox.Show(
                        this,
                        msg,
                        Text,
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    if (confirmBatch != DialogResult.OK) return;

                    // バックアップ（必要な場合のみ）
                    string? runFolder = null;
                    if (chkBackup.Checked)
                        runFolder = _backup.CreateBackupRunFolder(dir);

                    // ★同階層のみ（再帰しない）
                    var files = Directory.EnumerateFiles(dir, "*.osu", SearchOption.TopDirectoryOnly).ToList();

                    int ok = 0;
                    int ng = 0;
                    foreach (var file in files)
                    {
                        if (chkBackup.Checked && runFolder != null)
                            _backup.BackupFileTo(file, runFolder);

                        var r = OverwriteHelper.AlignFileOverwriteSafe(file, opt);
                        if (r.Success) ok++;
                        else ng++;
                    }

                    btnOpenBackup.Enabled = _backup.CanOpenLastFolder();

                    MessageBox.Show(this,
                        $"{Resources.DoneBatch}: {ok}/{files.Count}\n{Resources.Failed}: {ng}",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed record LangItem(string Display, string CultureName)
        {
            public override string ToString() => Display;
        }
    }
}
