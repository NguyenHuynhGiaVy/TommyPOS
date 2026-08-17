using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TommyPOS.Models;
using TommyPOS.Services;

namespace TommyPOS.Forms
{
    /// <summary>
    /// Form quản lý Size của một sản phẩm cụ thể (hoặc sizes mặc định toàn hệ thống nếu productId=0).
    /// Cho phép Thêm / Xóa size và phụ thu tương ứng.
    /// </summary>
    public class ProductSizeForm : Form
    {
        private readonly PosService _posService;
        private readonly int _productId;
        private readonly string _productName;

        private DataGridView dgvSizes = null!;
        private TextBox txtSizeLabel = null!;
        private TextBox txtPriceExtra = null!;
        private NumericUpDown numOrder = null!;

        public ProductSizeForm(PosService posService, int productId, string productName)
        {
            _posService = posService;
            _productId = productId;
            _productName = productName;
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            string title = _productId == 0
                ? "Quản Lý Size Mặc Định (Áp dụng tất cả món)"
                : $"Quản Lý Size – {_productName}";

            Text = title;
            Size = new Size(580, 530);
            MinimumSize = new Size(500, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(248, 245, 240);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(43, 24, 16) };
            var lblTitle = new Label
            {
                Text = title.ToUpper(),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 200, 140),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // Bottom Add Panel
            var pnlAdd = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                BackColor = Color.FromArgb(240, 237, 232),
                Padding = new Padding(10)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Labels row
            tbl.Controls.Add(new Label { Text = "Tên Size *", Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true }, 0, 0);
            tbl.Controls.Add(new Label { Text = "Phụ thu (đ) *", Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true }, 1, 0);
            tbl.Controls.Add(new Label { Text = "Thứ tự", Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true }, 2, 0);

            // Inputs row
            txtSizeLabel = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), PlaceholderText = "S, M, L, XL..." };
            txtPriceExtra = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), PlaceholderText = "0" };
            numOrder = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 999, Value = 1, Font = new Font("Segoe UI", 10) };

            var btnAdd = new Button
            {
                Text = "➕ Thêm Size",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 140, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            tbl.Controls.Add(txtSizeLabel, 0, 1);
            tbl.Controls.Add(txtPriceExtra, 1, 1);
            tbl.Controls.Add(numOrder, 2, 1);
            tbl.Controls.Add(btnAdd, 3, 1);

            pnlAdd.Controls.Add(tbl);
            Controls.Add(pnlAdd);

            // DataGridView
            dgvSizes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 36 }
            };
            dgvSizes.Columns.Add("Id", "ID"); dgvSizes.Columns["Id"]!.Visible = false;
            dgvSizes.Columns.Add("SizeLabel", "Tên Size");
            dgvSizes.Columns.Add("PriceExtra", "Phụ thu");
            dgvSizes.Columns.Add("DisplayOrder", "Thứ tự");

            var colDel = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "",
                Text = "🗑 Xóa",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                Width = 70
            };
            dgvSizes.Columns.Add(colDel);
            dgvSizes.CellClick += DgvSizes_CellClick;

            Controls.Add(dgvSizes);

            // Hint label
            if (_productId == 0)
            {
                var lblHint = new Label
                {
                    Text = "ℹ️ Sizes mặc định áp dụng cho các món chưa có size riêng.",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = Color.DimGray,
                    Dock = DockStyle.Top,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0)
                };
                Controls.Add(lblHint);
            }
        }

        private void LoadData()
        {
            dgvSizes.Rows.Clear();
            List<ProductSize> sizes = _productId == 0
                ? _posService.GetAllGlobalSizes()
                : _posService.GetSizesForProduct(_productId);

            foreach (var s in sizes)
            {
                dgvSizes.Rows.Add(s.Id, s.SizeLabel, $"{s.PriceExtra:N0}đ", s.DisplayOrder);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSizeLabel.Text))
            {
                MessageBox.Show("Vui lòng nhập tên size!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPriceExtra.Text.Trim().Replace(",", "").Replace(".", ""), out decimal extra) || extra < 0)
            {
                MessageBox.Show("Phụ thu không hợp lệ (ví dụ: 5000)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newSize = new ProductSize
            {
                ProductId = _productId,
                SizeLabel = txtSizeLabel.Text.Trim().ToUpper(),
                PriceExtra = extra,
                DisplayOrder = (int)numOrder.Value
            };
            _posService.SaveProductSize(newSize);

            txtSizeLabel.Clear();
            txtPriceExtra.Clear();
            numOrder.Value = 1;
            LoadData();
        }

        private void DgvSizes_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvSizes.Columns[e.ColumnIndex].Name != "Delete") return;

            int sizeId = Convert.ToInt32(dgvSizes.Rows[e.RowIndex].Cells["Id"].Value);
            var confirm = MessageBox.Show("Xóa size này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                _posService.DeleteProductSize(sizeId);
                LoadData();
            }
        }
    }
}
