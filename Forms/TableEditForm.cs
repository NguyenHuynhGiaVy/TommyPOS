using System;
using System.Drawing;
using System.Windows.Forms;
using TommyPOS.Models;

namespace TommyPOS.Forms
{
    public class TableEditForm : Form
    {
        public DiningTable EditingTable { get; private set; }

        private TextBox txtName = null!;
        private NumericUpDown numCap = null!;

        public TableEditForm(DiningTable? table = null)
        {
            EditingTable = table != null
                ? new DiningTable { Id = table.Id, Name = table.Name, Capacity = table.Capacity, Status = table.Status }
                : new DiningTable();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = EditingTable.Id == 0 ? "Thêm Bàn Mới" : "Sửa Thông Tin Bàn";
            Size = new Size(400, 260);
            MinimumSize = new Size(380, 240);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(248, 245, 240);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(43, 24, 16) };
            var lblTitle = new Label
            {
                Text = Text.ToUpper(),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
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
                Size = new Size(352, 105),
                ColumnCount = 2,
                RowCount = 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var lblName = new Label
            {
                Text = "Tên Bàn:",
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 0)
            };
            txtName = new TextBox
            {
                Text = EditingTable.Name,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 10, 0, 0)
            };

            var lblCap = new Label
            {
                Text = "Sức Chứa:",
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 0)
            };
            numCap = new NumericUpDown
            {
                Value = EditingTable.Capacity > 0 ? EditingTable.Capacity : 4,
                Minimum = 1,
                Maximum = 50,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 10, 0, 0)
            };

            tbl.Controls.Add(lblName, 0, 0);
            tbl.Controls.Add(txtName, 1, 0);
            tbl.Controls.Add(lblCap, 0, 1);
            tbl.Controls.Add(numCap, 1, 1);
            Controls.Add(tbl);

            var btnSave = new Button
            {
                Text = "💾 LƯU BÀN",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                Location = new Point(215, 180),
                Size = new Size(140, 42),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên bàn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                EditingTable.Name = txtName.Text.Trim();
                EditingTable.Capacity = (int)numCap.Value;
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(100, 180),
                Size = new Size(105, 42),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }
    }
}
