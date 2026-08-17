using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TommyPOS.Models;
using TommyPOS.Services;

namespace TommyPOS.Forms
{
    public class ProductEditForm : Form
    {
        public Product EditingProduct { get; private set; }
        private readonly List<Category> _categories;
        private readonly PosService _posService;

        private TextBox txtName = null!;
        private ComboBox cboCategory = null!;
        private TextBox txtPrice = null!;
        private TextBox txtDesc = null!;
        private CheckBox chkAvailable = null!;
        private PictureBox picPreview = null!;
        private Button btnChooseImg = null!;
        private Button btnClearImg = null!;

        public ProductEditForm(Product? product, List<Category> categories, PosService posService)
        {
            _categories = categories;
            _posService = posService;
            EditingProduct = product != null
                ? new Product
                {
                    Id = product.Id, Name = product.Name, CategoryId = product.CategoryId,
                    CategoryName = product.CategoryName, Price = product.Price,
                    Description = product.Description, IsAvailable = product.IsAvailable,
                    ImageUrl = product.ImageUrl
                }
                : new Product { IsAvailable = true };
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = EditingProduct.Id == 0 ? "Thêm Món Mới" : "Sửa Thông Tin Món";
            Size = new Size(540, 560);
            MinimumSize = new Size(500, 520);
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

            // Body Panel with AutoScroll to prevent clipping
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 12)
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            // TableLayoutPanel
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 6
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // Name
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // Category
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // Price
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 75)); // Description
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Available
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); // Image

            // Row 0 – Tên món
            tbl.Controls.Add(MakeLabel("Tên món:"), 0, 0);
            txtName = new TextBox { Text = EditingProduct.Name, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 8, 0, 0) };
            tbl.Controls.Add(txtName, 1, 0);

            // Row 1 – Danh mục
            tbl.Controls.Add(MakeLabel("Danh mục:"), 0, 1);
            cboCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 8, 0, 0) };
            foreach (var cat in _categories)
            {
                cboCategory.Items.Add(cat);
                if (cat.Id == EditingProduct.CategoryId) cboCategory.SelectedItem = cat;
            }
            cboCategory.DisplayMember = "Name";
            cboCategory.ValueMember = "Id";
            if (cboCategory.SelectedIndex < 0 && cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            tbl.Controls.Add(cboCategory, 1, 1);

            // Row 2 – Giá
            tbl.Controls.Add(MakeLabel("Giá bán (đ):"), 0, 2);
            txtPrice = new TextBox { Text = EditingProduct.Price.ToString("0"), Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 8, 0, 0) };
            tbl.Controls.Add(txtPrice, 1, 2);

            // Row 3 – Mô tả
            tbl.Controls.Add(MakeLabel("Mô tả:"), 0, 3);
            txtDesc = new TextBox { Text = EditingProduct.Description, Dock = DockStyle.Fill, Multiline = true, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 4, 0, 0), ScrollBars = ScrollBars.Vertical };
            tbl.Controls.Add(txtDesc, 1, 3);

            // Row 4 – Đang bán
            tbl.Controls.Add(MakeLabel("Trạng thái:"), 0, 4);
            chkAvailable = new CheckBox { Checked = EditingProduct.IsAvailable, Text = "Đang mở bán", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 80, 30), Margin = new Padding(0, 8, 0, 0) };
            tbl.Controls.Add(chkAvailable, 1, 4);

            // Row 5 – Hình ảnh
            tbl.Controls.Add(MakeLabel("Hình ảnh:"), 0, 5);

            var pnlImgContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
            picPreview = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            LoadImagePreview(EditingProduct.ImageUrl);

            btnChooseImg = new Button
            {
                Text = "📸 Chọn Ảnh...",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(90, 10),
                Size = new Size(115, 32),
                Cursor = Cursors.Hand
            };
            btnChooseImg.FlatAppearance.BorderSize = 0;
            btnChooseImg.Click += BtnChooseImg_Click;

            btnClearImg = new Button
            {
                Text = "🗑 Xóa",
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.FromArgb(200, 190, 180),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(210, 10),
                Size = new Size(65, 32),
                Cursor = Cursors.Hand
            };
            btnClearImg.FlatAppearance.BorderSize = 0;
            btnClearImg.Click += (s, e) =>
            {
                EditingProduct.ImageUrl = "";
                picPreview.Image = null;
            };

            pnlImgContainer.Controls.Add(picPreview);
            pnlImgContainer.Controls.Add(btnChooseImg);
            pnlImgContainer.Controls.Add(btnClearImg);
            tbl.Controls.Add(pnlImgContainer, 1, 5);

            pnlBody.Controls.Add(tbl);

            // Footer Panel for action buttons
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(238, 233, 226), Padding = new Padding(16, 10, 16, 10) };

            if (EditingProduct.Id > 0)
            {
                var btnSizes = new Button
                {
                    Text = "📐 Quản Lý Sizes",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(70, 110, 180),
                    ForeColor = Color.White,
                    Location = new Point(16, 11),
                    Size = new Size(130, 38),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnSizes.FlatAppearance.BorderSize = 0;
                btnSizes.Click += (s, e) =>
                {
                    using var szForm = new ProductSizeForm(_posService, EditingProduct.Id, EditingProduct.Name);
                    szForm.ShowDialog();
                };
                pnlFooter.Controls.Add(btnSizes);
            }

            var btnSave = new Button
            {
                Text = "💾 LƯU MÓN",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                Location = new Point(pnlFooter.Width - 145, 11),
                Size = new Size(130, 38),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(pnlFooter.Width - 245, 11),
                Size = new Size(90, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);
            Controls.Add(pnlFooter);
        }

        private void LoadImagePreview(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                picPreview.Image = null;
                return;
            }

            string fullPath = Path.IsPathRooted(imagePath)
                ? imagePath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);

            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), imagePath);
            }

            if (File.Exists(fullPath))
            {
                try
                {
                    using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    picPreview.Image = Image.FromStream(stream);
                }
                catch { picPreview.Image = null; }
            }
            else
            {
                picPreview.Image = null;
            }
        }

        private void BtnChooseImg_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn hình ảnh sản phẩm",
                Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Tất cả tệp (*.*)|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Products");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    string ext = Path.GetExtension(ofd.FileName);
                    string newFileName = $"prod_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
                    string targetPath = Path.Combine(targetDir, newFileName);

                    File.Copy(ofd.FileName, targetPath, true);

                    EditingProduct.ImageUrl = Path.Combine("Assets", "Products", newFileName);
                    LoadImagePreview(EditingProduct.ImageUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu hình ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(43, 24, 16),
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên món!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price <= 0)
            {
                MessageBox.Show("Giá bán không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selCat = cboCategory.SelectedItem as Category;
            if (selCat == null) return;

            EditingProduct.Name = txtName.Text.Trim();
            EditingProduct.CategoryId = selCat.Id;
            EditingProduct.CategoryName = selCat.Name;
            EditingProduct.Price = price;
            EditingProduct.Description = txtDesc.Text.Trim();
            EditingProduct.IsAvailable = chkAvailable.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
