using System;
using System.Drawing;
using System.Windows.Forms;
using TommyPOS.Models;

namespace TommyPOS.Forms
{
    /// <summary>Form Thêm / Sửa Danh Mục sản phẩm.</summary>
    public class CategoryEditForm : Form
    {
        public Category EditingCategory { get; private set; }

        private TextBox txtName = null!;
        private ComboBox cboIcon = null!;
        private NumericUpDown numOrder = null!;

        private static readonly string[] IconOptions =
        {
            "☕", "🥛", "🍑", "🥤", "🥐", "🍵", "🧃", "🍰", "🧋", "🍫",
            "🍹", "🧊", "🍊", "🍋", "🍓", "🫖", "🎂", "🥗", "🍜", "🍱"
        };

        public CategoryEditForm(Category? category = null)
        {
            EditingCategory = category != null
                ? new Category { Id = category.Id, Name = category.Name, Icon = category.Icon, DisplayOrder = category.DisplayOrder }
                : new Category();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = EditingCategory.Id == 0 ? "Thêm Danh Mục Mới" : "Sửa Danh Mục";
            Size = new Size(440, 320);
            MinimumSize = new Size(410, 290);
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

            // Body dùng TableLayoutPanel để không bị đè nhau
            var tbl = new TableLayoutPanel
            {
                Location = new Point(16, 62),
                Size = new Size(392, 155),
                ColumnCount = 2,
                RowCount = 3,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            // Row 0 – Tên danh mục
            var lblName = new Label { Text = "Tên danh mục:", Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            txtName = new TextBox { Text = EditingCategory.Name, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 10, 0, 0) };
            tbl.Controls.Add(lblName, 0, 0);
            tbl.Controls.Add(txtName, 1, 0);

            // Row 1 – Icon
            var lblIcon = new Label { Text = "Biểu tượng:", Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            cboIcon = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13), Margin = new Padding(0, 8, 0, 0) };
            cboIcon.Items.AddRange(IconOptions);
            cboIcon.SelectedItem = IconOptions[0];
            foreach (var ic in IconOptions)
            {
                if (ic == EditingCategory.Icon) { cboIcon.SelectedItem = ic; break; }
            }
            tbl.Controls.Add(lblIcon, 0, 1);
            tbl.Controls.Add(cboIcon, 1, 1);

            // Row 2 – Thứ tự
            var lblOrder = new Label { Text = "Thứ tự HT:", Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            numOrder = new NumericUpDown { Minimum = 1, Maximum = 999, Value = EditingCategory.DisplayOrder > 0 ? EditingCategory.DisplayOrder : 1, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 8, 0, 0) };
            tbl.Controls.Add(lblOrder, 0, 2);
            tbl.Controls.Add(numOrder, 1, 2);

            Controls.Add(tbl);

            // Buttons
            var btnSave = new Button
            {
                Text = "💾 LƯU",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                Location = new Point(240, 245),
                Size = new Size(140, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(130, 245),
                Size = new Size(100, 40),
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
                MessageBox.Show("Vui lòng nhập tên danh mục!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            EditingCategory.Name = txtName.Text.Trim();
            EditingCategory.Icon = cboIcon.SelectedItem?.ToString() ?? "☕";
            EditingCategory.DisplayOrder = (int)numOrder.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
