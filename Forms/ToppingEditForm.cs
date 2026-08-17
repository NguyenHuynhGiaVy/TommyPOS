using System;
using System.Drawing;
using System.Windows.Forms;
using TommyPOS.Models;

namespace TommyPOS.Forms
{
    /// <summary>Form Thêm / Sửa Topping.</summary>
    public class ToppingEditForm : Form
    {
        public ToppingItem EditingTopping { get; private set; }

        private TextBox txtName = null!;
        private TextBox txtPrice = null!;
        private CheckBox chkAvailable = null!;

        public ToppingEditForm(ToppingItem? topping = null)
        {
            EditingTopping = topping != null
                ? new ToppingItem { Id = topping.Id, Name = topping.Name, Price = topping.Price, IsAvailable = topping.IsAvailable }
                : new ToppingItem { IsAvailable = true };
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = EditingTopping.Id == 0 ? "Thêm Topping Mới" : "Sửa Topping";
            Size = new Size(420, 300);
            MinimumSize = new Size(390, 270);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(248, 245, 240);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(43, 24, 16) };
            var lblTitle = new Label
            {
                Text = Text.ToUpper(),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 200, 140),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // TableLayoutPanel body
            var tbl = new TableLayoutPanel
            {
                Location = new Point(16, 62),
                Size = new Size(372, 150),
                ColumnCount = 2,
                RowCount = 3,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            // Row 0 – Tên
            var lblName = new Label { Text = "Tên topping:", Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            txtName = new TextBox { Text = EditingTopping.Name, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 10, 0, 0) };
            tbl.Controls.Add(lblName, 0, 0);
            tbl.Controls.Add(txtName, 1, 0);

            // Row 1 – Giá
            var lblPrice = new Label { Text = "Giá thêm (đ):", Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            txtPrice = new TextBox { Text = EditingTopping.Price.ToString("0"), Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 10, 0, 0) };
            tbl.Controls.Add(lblPrice, 0, 1);
            tbl.Controls.Add(txtPrice, 1, 1);

            // Row 2 – Có bán không
            var lblAvail = new Label { Text = "Đang bán:", Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            chkAvailable = new CheckBox { Checked = EditingTopping.IsAvailable, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), Text = "Có", Margin = new Padding(0, 10, 0, 0) };
            tbl.Controls.Add(lblAvail, 0, 2);
            tbl.Controls.Add(chkAvailable, 1, 2);

            Controls.Add(tbl);

            // Buttons
            var btnSave = new Button
            {
                Text = "💾 LƯU",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                Location = new Point(230, 225),
                Size = new Size(130, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(115, 225),
                Size = new Size(105, 40),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên topping!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price < 0)
            {
                MessageBox.Show("Giá topping không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            EditingTopping.Name = txtName.Text.Trim();
            EditingTopping.Price = price;
            EditingTopping.IsAvailable = chkAvailable.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
