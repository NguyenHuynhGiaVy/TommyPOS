using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TommyPOS.Models;
using TommyPOS.Services;

namespace TommyPOS.Forms
{
    /// <summary>Form tùy chỉnh size, đường, đá, topping khi thêm món vào đơn.</summary>
    public class CustomizationForm : Form
    {
        private readonly Product _product;
        private readonly PosService _posService;
        public OrderDetail SelectedDetail { get; private set; }

        // Size – generated dynamically
        private Panel pnlSizeButtons = null!;
        private readonly List<Button> _sizeBtns = new();
        private List<ProductSize> _sizes = new();
        private int _selectedSizeIndex = 0;

        private ComboBox cboSugar = null!;
        private ComboBox cboIce = null!;

        // Toppings – generated dynamically
        private FlowLayoutPanel flpToppings = null!;
        private readonly List<(CheckBox chk, ToppingItem top)> _toppingChecks = new();

        private TextBox txtNote = null!;
        private NumericUpDown numQuantity = null!;
        private Label lblTotal = null!;

        public CustomizationForm(Product product, PosService posService, OrderDetail? existingDetail = null)
        {
            _product = product;
            _posService = posService;
            SelectedDetail = existingDetail ?? new OrderDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1,
                Sugar = "100%",
                Ice = "Bình thường"
            };

            InitializeUI();
            RecalculateTotal();
        }

        private void InitializeUI()
        {
            Text = $"Tùy Chỉnh – {_product.Name}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(490, 580);
            BackColor = Color.FromArgb(248, 245, 240);

            // Load data
            _sizes = _posService.GetSizesForProduct(_product.Id);
            if (_sizes.Count == 0)
            {
                // Fallback if DB empty for some reason
                _sizes = new List<ProductSize>
                {
                    new() { SizeLabel = "S", PriceExtra = 0, DisplayOrder = 1 },
                    new() { SizeLabel = "M", PriceExtra = 5000, DisplayOrder = 2 },
                    new() { SizeLabel = "L", PriceExtra = 10000, DisplayOrder = 3 }
                };
            }

            // Pre-select size matching existing detail
            _selectedSizeIndex = 0;
            for (int i = 0; i < _sizes.Count; i++)
            {
                if (_sizes[i].SizeLabel == SelectedDetail.Size) { _selectedSizeIndex = i; break; }
            }

            var toppings = _posService.GetToppings(availableOnly: true);

            // ──── Build layout using Panel + Controls ────────────────────────
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(74, 44, 42) };
            var lblTitle = new Label
            {
                Text = _product.Name,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // Scrollable body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15, 10, 15, 10)
            };
            Controls.Add(pnlBody);

            int y = 10;
            int bodyWidth = 440; // inner width reference

            // 1. Size group
            var grpSize = new GroupBox
            {
                Text = "1. Chọn Size",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 44, 42),
                Location = new Point(0, y),
                Width = bodyWidth,
                Height = 70
            };

            pnlSizeButtons = new Panel { Location = new Point(10, 22), Size = new Size(bodyWidth - 20, 38) };

            int bx = 0;
            for (int i = 0; i < _sizes.Count; i++)
            {
                var sz = _sizes[i];
                int idx = i;
                string label = sz.PriceExtra > 0 ? $"{sz.SizeLabel} (+{sz.PriceExtra / 1000:0}k)" : sz.SizeLabel;
                var btn = new Button
                {
                    Text = label,
                    Location = new Point(bx, 0),
                    Size = new Size(Math.Max(90, label.Length * 9 + 20), 36),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = idx
                };
                UpdateSizeButton(btn, idx == _selectedSizeIndex);
                btn.Click += (s, e) =>
                {
                    _selectedSizeIndex = idx;
                    RefreshSizeButtons();
                    RecalculateTotal();
                };
                pnlSizeButtons.Controls.Add(btn);
                _sizeBtns.Add(btn);
                bx += btn.Width + 8;
            }

            grpSize.Controls.Add(pnlSizeButtons);
            pnlBody.Controls.Add(grpSize);
            y += grpSize.Height + 10;

            // 2. Sugar & Ice
            var grpSugarIce = new GroupBox
            {
                Text = "2. Mức Đường & Đá",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 44, 42),
                Location = new Point(0, y),
                Width = bodyWidth,
                Height = 75
            };

            var tblSI = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Padding = new Padding(8, 20, 8, 5) };
            tblSI.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            tblSI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tblSI.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35));
            tblSI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var lblSugar = new Label { Text = "Đường:", Anchor = AnchorStyles.Left | AnchorStyles.Top, AutoSize = true, Font = new Font("Segoe UI", 9) };
            cboSugar = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cboSugar.Items.AddRange(new object[] { "0%", "30%", "50%", "70%", "100%" });
            cboSugar.SelectedItem = SelectedDetail.Sugar;
            if (cboSugar.SelectedIndex < 0) cboSugar.SelectedIndex = 4;
            cboSugar.SelectedIndexChanged += (s, e) => RecalculateTotal();

            var lblIce = new Label { Text = "Đá:", Anchor = AnchorStyles.Left | AnchorStyles.Top, AutoSize = true, Font = new Font("Segoe UI", 9) };
            cboIce = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cboIce.Items.AddRange(new object[] { "Không đá", "Ít đá (50%)", "Bình thường" });
            cboIce.SelectedItem = SelectedDetail.Ice;
            if (cboIce.SelectedIndex < 0) cboIce.SelectedIndex = 2;
            cboIce.SelectedIndexChanged += (s, e) => RecalculateTotal();

            tblSI.Controls.Add(lblSugar, 0, 0);
            tblSI.Controls.Add(cboSugar, 1, 0);
            tblSI.Controls.Add(lblIce, 2, 0);
            tblSI.Controls.Add(cboIce, 3, 0);
            grpSugarIce.Controls.Add(tblSI);
            pnlBody.Controls.Add(grpSugarIce);
            y += grpSugarIce.Height + 10;

            // 3. Toppings (dynamic from DB)
            int toppingRows = Math.Max(1, (toppings.Count + 1) / 2);
            int grpToppingH = 30 + toppingRows * 30 + 10;

            var grpTopping = new GroupBox
            {
                Text = "3. Topping Thêm",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 44, 42),
                Location = new Point(0, y),
                Width = bodyWidth,
                Height = grpToppingH
            };

            flpToppings = new FlowLayoutPanel
            {
                Location = new Point(8, 22),
                Size = new Size(bodyWidth - 16, grpToppingH - 28),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false
            };

            foreach (var top in toppings)
            {
                string topText = $"{top.Name} (+{top.Price / 1000:0}k)";
                var chk = new CheckBox
                {
                    Text = topText,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9),
                    Margin = new Padding(4, 4, 15, 4)
                };

                // Check if this topping was in existing detail
                if (!string.IsNullOrEmpty(SelectedDetail.Toppings) && SelectedDetail.Toppings.Contains(top.Name))
                {
                    chk.Checked = true;
                }

                chk.CheckedChanged += (s, e) => RecalculateTotal();
                _toppingChecks.Add((chk, top));
                flpToppings.Controls.Add(chk);
            }

            grpTopping.Controls.Add(flpToppings);
            pnlBody.Controls.Add(grpTopping);
            y += grpTopping.Height + 10;

            // 4. Quantity & Note
            var grpQty = new GroupBox
            {
                Text = "4. Số Lượng & Ghi Chú",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 44, 42),
                Location = new Point(0, y),
                Width = bodyWidth,
                Height = 65
            };

            var tblQty = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Padding = new Padding(8, 18, 8, 5) };
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
            tblQty.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblQty = new Label { Text = "Số lượng:", Anchor = AnchorStyles.Left | AnchorStyles.Top, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            numQuantity = new NumericUpDown { Minimum = 1, Maximum = 99, Value = SelectedDetail.Quantity, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            numQuantity.ValueChanged += (s, e) => RecalculateTotal();

            var lblNoteStr = new Label { Text = "Ghi chú:", Anchor = AnchorStyles.Left | AnchorStyles.Top, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtNote = new TextBox { Text = SelectedDetail.Note, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            tblQty.Controls.Add(lblQty, 0, 0);
            tblQty.Controls.Add(numQuantity, 1, 0);
            tblQty.Controls.Add(lblNoteStr, 2, 0);
            tblQty.Controls.Add(txtNote, 3, 0);
            grpQty.Controls.Add(tblQty);
            pnlBody.Controls.Add(grpQty);
            y += grpQty.Height + 8;

            // 5. Total label
            lblTotal = new Label
            {
                Text = "Tổng: 0đ",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 40, 40),
                Location = new Point(0, y),
                Size = new Size(bodyWidth, 30),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlBody.Controls.Add(lblTotal);
            y += 38;

            // 6. Buttons
            var btnConfirm = new Button
            {
                Text = "✔ XÁC NHẬN MÓN",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(bodyWidth - 205, y),
                Size = new Size(205, 44),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            var btnCancel = new Button
            {
                Text = "HỦY BỎ",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(220, 220, 220),
                Location = new Point(bodyWidth - 330, y),
                Size = new Size(115, 44),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            pnlBody.Controls.Add(btnConfirm);
            pnlBody.Controls.Add(btnCancel);

            y += 55;

            // Set form height to fit content (capped at screen height)
            int formH = Math.Min(y + 65 + 30, Screen.PrimaryScreen!.WorkingArea.Height - 40);
            Size = new Size(bodyWidth + 50, formH);
        }

        private void UpdateSizeButton(Button btn, bool selected)
        {
            btn.BackColor = selected ? Color.FromArgb(74, 44, 42) : Color.FromArgb(230, 225, 218);
            btn.ForeColor = selected ? Color.White : Color.FromArgb(50, 30, 25);
            btn.FlatAppearance.BorderColor = selected ? Color.FromArgb(74, 44, 42) : Color.FromArgb(180, 160, 140);
            btn.FlatAppearance.BorderSize = 1;
        }

        private void RefreshSizeButtons()
        {
            for (int i = 0; i < _sizeBtns.Count; i++)
                UpdateSizeButton(_sizeBtns[i], i == _selectedSizeIndex);
        }

        private void RecalculateTotal()
        {
            // Size
            decimal sizeExtra = 0;
            string sizeStr = "S";
            if (_selectedSizeIndex >= 0 && _selectedSizeIndex < _sizes.Count)
            {
                sizeExtra = _sizes[_selectedSizeIndex].PriceExtra;
                sizeStr = _sizes[_selectedSizeIndex].SizeLabel;
            }

            // Toppings
            decimal toppingExtra = 0;
            var toppingList = new List<string>();
            foreach (var (chk, top) in _toppingChecks)
            {
                if (chk.Checked)
                {
                    toppingExtra += top.Price;
                    toppingList.Add($"{top.Name} (+{top.Price / 1000:0}k)");
                }
            }

            SelectedDetail.Size = sizeStr;
            SelectedDetail.SizePriceExtra = sizeExtra;
            SelectedDetail.Sugar = cboSugar.SelectedItem?.ToString() ?? "100%";
            SelectedDetail.Ice = cboIce.SelectedItem?.ToString() ?? "Bình thường";
            SelectedDetail.Toppings = string.Join(", ", toppingList);
            SelectedDetail.ToppingPriceExtra = toppingExtra;
            SelectedDetail.Quantity = (int)numQuantity.Value;
            SelectedDetail.Note = txtNote.Text.Trim();

            lblTotal.Text = $"Tổng tiền món: {SelectedDetail.SubTotal:N0}đ";
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            RecalculateTotal();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
