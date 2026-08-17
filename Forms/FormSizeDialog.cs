using System;
using System.Drawing;
using System.Windows.Forms;
using TommyPOS.Utils;

namespace TommyPOS.Forms
{
    public class FormSizeDialog : Form
    {
        private readonly Form _targetForm;
        private NumericUpDown numWidth = null!;
        private NumericUpDown numHeight = null!;
        private CheckBox chkMaximized = null!;
        private Label lblCurrentInfo = null!;

        public FormSizeDialog(Form targetForm)
        {
            _targetForm = targetForm ?? throw new ArgumentNullException(nameof(targetForm));
            InitializeUI();
            UpdateCurrentInfoLabel();
        }

        private void InitializeUI()
        {
            Text = "Điều Chỉnh Kích Thước Giao Diện";
            Size = new Size(540, 480);
            MinimumSize = new Size(500, 440);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(248, 245, 240);

            // 1. Header Panel
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(43, 24, 16) };
            var lblTitle = new Label
            {
                Text = "📐 ĐIỀU CHỈNH KÍCH THƯỚC GIAO DIỆN",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 200, 140),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // Main Content Container
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15),
                AutoScroll = true
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            int top = 10;

            // Current info banner
            lblCurrentInfo = new Label
            {
                Location = new Point(15, top),
                Size = new Size(490, 32),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 35, 25),
                BackColor = Color.FromArgb(235, 225, 215),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlBody.Controls.Add(lblCurrentInfo);
            top += 42;

            // Section 1: Presets
            var lblPresetsGroup = new Label
            {
                Text = "1. Chọn nhanh độ phân giải có sẵn:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(43, 24, 16),
                Location = new Point(15, top),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblPresetsGroup);
            top += 25;

            var pnlPresets = new FlowLayoutPanel
            {
                Location = new Point(15, top),
                Size = new Size(490, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = false,
                WrapContents = true
            };

            var presets = new (string label, int w, int h)[]
            {
                ("📱 1024 x 768 (Nhỏ)", 1024, 768),
                ("💻 1280 x 720 (HD)", 1280, 720),
                ("☕ 1400 x 820 (Mặc định)", 1400, 820),
                ("🖥️ 1600 x 900 (HD+)", 1600, 900),
                ("📺 1920 x 1080 (FHD)", 1920, 1080)
            };

            foreach (var (label, w, h) in presets)
            {
                var btnPreset = new Button
                {
                    Text = label,
                    Size = new Size(155, 34),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(50, 30, 20),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3, 3, 3, 3)
                };
                btnPreset.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);
                btnPreset.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 230, 218);

                int targetW = w;
                int targetH = h;
                btnPreset.Click += (s, e) =>
                {
                    chkMaximized.Checked = false;
                    numWidth.Value = Math.Clamp(targetW, numWidth.Minimum, numWidth.Maximum);
                    numHeight.Value = Math.Clamp(targetH, numHeight.Minimum, numHeight.Maximum);
                    ApplySize(targetW, targetH, false);
                };
                pnlPresets.Controls.Add(btnPreset);
            }
            pnlBody.Controls.Add(pnlPresets);
            top += 90;

            // Section 2: Custom dimensions
            var lblCustomGroup = new Label
            {
                Text = "2. Tùy chỉnh kích thước thủ công (pixels):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(43, 24, 16),
                Location = new Point(15, top),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblCustomGroup);
            top += 25;

            var pnlCustom = new TableLayoutPanel
            {
                Location = new Point(15, top),
                Size = new Size(490, 42),
                ColumnCount = 4,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlCustom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            pnlCustom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlCustom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            pnlCustom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            int initW = _targetForm.WindowState == FormWindowState.Maximized ? _targetForm.RestoreBounds.Width : _targetForm.Width;
            int initH = _targetForm.WindowState == FormWindowState.Maximized ? _targetForm.RestoreBounds.Height : _targetForm.Height;

            var lblW = new Label { Text = "Chiều rộng:", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.Black, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            numWidth = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 3840,
                Increment = 20,
                Value = Math.Clamp(initW > 100 ? initW : 1400, 1000, 3840),
                Font = new Font("Segoe UI", 10f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            var lblH = new Label { Text = "Chiều cao:", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.Black, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            numHeight = new NumericUpDown
            {
                Minimum = 650,
                Maximum = 2160,
                Increment = 20,
                Value = Math.Clamp(initH > 100 ? initH : 820, 650, 2160),
                Font = new Font("Segoe UI", 10f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            pnlCustom.Controls.Add(lblW, 0, 0);
            pnlCustom.Controls.Add(numWidth, 1, 0);
            pnlCustom.Controls.Add(lblH, 2, 0);
            pnlCustom.Controls.Add(numHeight, 3, 0);
            pnlBody.Controls.Add(pnlCustom);
            top += 48;

            // Section 3: Maximized checkbox
            chkMaximized = new CheckBox
            {
                Text = "🖥️ Phóng to tối đa cửa sổ (Maximized toàn màn hình)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(43, 24, 16),
                Location = new Point(15, top),
                AutoSize = true,
                Checked = _targetForm.WindowState == FormWindowState.Maximized,
                Cursor = Cursors.Hand
            };
            chkMaximized.CheckedChanged += (s, e) =>
            {
                numWidth.Enabled = !chkMaximized.Checked;
                numHeight.Enabled = !chkMaximized.Checked;
            };
            pnlBody.Controls.Add(chkMaximized);
            top += 38;

            // Action Buttons Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(238, 233, 226),
                Padding = new Padding(15, 10, 15, 10)
            };

            var btnApply = new Button
            {
                Text = "✓ Áp Dụng",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(43, 24, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 38),
                Location = new Point(15, 11),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) =>
            {
                int w = (int)numWidth.Value;
                int h = (int)numHeight.Value;
                bool max = chkMaximized.Checked;
                ApplySize(w, h, max);
            };

            var btnReset = new Button
            {
                Text = "↺ Mặc Định",
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(220, 210, 200),
                ForeColor = Color.FromArgb(43, 24, 16),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 38),
                Location = new Point(145, 11),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) =>
            {
                chkMaximized.Checked = false;
                numWidth.Value = 1400;
                numHeight.Value = 820;
                ApplySize(1400, 820, false);
            };

            var btnClose = new Button
            {
                Text = "Đóng",
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(190, 180, 170),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(pnlFooter.Width - 115, 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            pnlFooter.Controls.Add(btnApply);
            pnlFooter.Controls.Add(btnReset);
            pnlFooter.Controls.Add(btnClose);
            Controls.Add(pnlFooter);
        }

        private void ApplySize(int width, int height, bool isMaximized)
        {
            if (isMaximized)
            {
                _targetForm.WindowState = FormWindowState.Maximized;
            }
            else
            {
                _targetForm.WindowState = FormWindowState.Normal;
                _targetForm.Size = new Size(width, height);
                _targetForm.StartPosition = FormStartPosition.CenterScreen;
            }

            FormConfig.SaveSettings(width, height, isMaximized);
            UpdateCurrentInfoLabel();
        }

        private void UpdateCurrentInfoLabel()
        {
            if (lblCurrentInfo == null) return;

            if (_targetForm.WindowState == FormWindowState.Maximized)
            {
                lblCurrentInfo.Text = "📏 Kích thước hiện tại: TOÀN MÀN HÌNH (Maximized)";
            }
            else
            {
                lblCurrentInfo.Text = $"📏 Kích thước hiện tại: {_targetForm.Width} x {_targetForm.Height} px";
            }
        }
    }
}
