using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TommyPOS.Controls;
using TommyPOS.Database;
using TommyPOS.Models;
using TommyPOS.Services;
using TommyPOS.Utils;

namespace TommyPOS.Forms
{
    public class MainForm : Form
    {
        private readonly PosService _posService;
        private Order? _currentOrder;
        private DiningTable? _selectedTable;

        // Header controls
        private Label lblClock = null!;
        private Label lblSelectedTableTitle = null!;
        private Panel pnlMainContent = null!;

        // View panels
        private Panel pnlPosView = null!;
        private Panel pnlTableMgmtView = null!;
        private Panel pnlMenuMgmtView = null!;
        private Panel pnlCategoryMgmtView = null!;
        private Panel pnlToppingMgmtView = null!;
        private Panel pnlOrderHistoryView = null!;
        private Panel pnlAnalyticsView = null!;

        // POS Controls
        private FlowLayoutPanel flpTables = null!;
        private FlowLayoutPanel flpCategories = null!;
        private FlowLayoutPanel flpProducts = null!;
        private TextBox txtSearchProduct = null!;
        private DataGridView dgvCart = null!;
        private Label lblCartSubTotal = null!;
        private Label lblCartDiscount = null!;
        private Label lblCartTotal = null!;
        private NumericUpDown numDiscountPct = null!;

        private int _selectedCategoryId = 0;

        // Management Grids
        private DataGridView dgvTableMgmt = null!;
        private DataGridView dgvMenuMgmt = null!;
        private DataGridView dgvCategoryMgmt = null!;
        private DataGridView dgvToppingMgmt = null!;
        private DataGridView dgvOrderHistory = null!;

        // Analytics
        private Label lblAnalyticsRevenue = null!;
        private Label lblAnalyticsOrders = null!;
        private Label lblAnalyticsAvg = null!;
        private Label lblCountCategories = null!;
        private Label lblCountProducts = null!;
        private Label lblCountTables = null!;
        private Label lblCountSizes = null!;
        private Label lblCountToppings = null!;
        private RevenueBarChart _revenueChart = null!;
        private string _currentPeriod = "day";

        // Nav button references for highlight
        private readonly List<Button> _navButtons = new();

        public MainForm()
        {
            _posService = new PosService();
            InitializeDatabase();
            InitializeComponentUI();
            SelectTakeaway();
        }

        private void InitializeDatabase()
        {
            try { DatabaseHelper.InitializeDatabase(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo CSDL: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponentUI()
        {
            Text = "TOMMY COFFEE POS – QUẢN LÝ & BÁN HÀNG CÀ PHÊ";
            Size = new Size(1400, 820);
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;
            BackColor = Color.FromArgb(248, 245, 240);

            FormConfig.ApplySettingsToForm(this);

            ResizeEnd += (s, e) =>
            {
                if (WindowState == FormWindowState.Normal)
                {
                    FormConfig.SaveSettings(Width, Height, false);
                }
                else if (WindowState == FormWindowState.Maximized)
                {
                    FormConfig.SaveSettings(RestoreBounds.Width, RestoreBounds.Height, true);
                }
            };

            FormClosing += (s, e) =>
            {
                bool isMax = WindowState == FormWindowState.Maximized;
                int w = isMax ? RestoreBounds.Width : Width;
                int h = isMax ? RestoreBounds.Height : Height;
                FormConfig.SaveSettings(w, h, isMax);
            };

            Controls.Add(CreateHeaderPanel());
            Controls.Add(CreateSidebarPanel());

            pnlMainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 245, 240) };
            Controls.Add(pnlMainContent);
            pnlMainContent.BringToFront();

            InitializePosView();
            InitializeTableMgmtView();
            InitializeMenuMgmtView();
            InitializeCategoryMgmtView();
            InitializeToppingMgmtView();
            InitializeOrderHistoryView();
            InitializeAnalyticsView();

            SwitchView(pnlPosView);

            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            timer.Start();
        }

        // ────────────────────────────────────────────────────────
        private Panel CreateHeaderPanel()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(43, 24, 16) };

            // Left Brand Section
            var pnlBrand = new Panel { Dock = DockStyle.Left, Width = 380, BackColor = Color.Transparent };

            var picLogo = new PictureBox { Location = new Point(14, 7), Size = new Size(50, 50), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
            if (!File.Exists(logoPath)) logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.png");
            if (File.Exists(logoPath)) { try { picLogo.Image = Image.FromFile(logoPath); } catch { } }
            pnlBrand.Controls.Add(picLogo);

            pnlBrand.Controls.Add(new Label { Text = "TOMMY COFFEE POS", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(240, 200, 140), Location = new Point(70, 8), AutoSize = true });
            pnlBrand.Controls.Add(new Label { Text = "Hệ thống Quản lý & Bán Cà Phê Chuyên Nghiệp", Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = Color.FromArgb(209, 199, 189), Location = new Point(72, 36), AutoSize = true });
            pnlHeader.Controls.Add(pnlBrand);

            // Right User & Clock & Size Section
            var pnlUserClock = new Panel { Dock = DockStyle.Right, Width = 380, BackColor = Color.Transparent, Padding = new Padding(0, 8, 16, 0) };

            var btnHeaderSize = new Button
            {
                Text = "📐 Kích Thước Form",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 200, 140),
                BackColor = Color.FromArgb(70, 38, 30),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 28),
                Location = new Point(234, 18),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnHeaderSize.FlatAppearance.BorderColor = Color.FromArgb(160, 120, 90);
            btnHeaderSize.Click += (s, e) => OpenFormSizeDialog();
            pnlUserClock.Controls.Add(btnHeaderSize);

            var lblCashier = new Label { Text = "👤 Thu Ngân: Vy Nguyen", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(0, 8), Size = new Size(225, 22), TextAlign = ContentAlignment.MiddleRight };
            lblClock = new Label { Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(209, 199, 189), Location = new Point(0, 32), Size = new Size(225, 20), TextAlign = ContentAlignment.MiddleRight };
            pnlUserClock.Controls.Add(lblClock);
            pnlUserClock.Controls.Add(lblCashier);
            pnlHeader.Controls.Add(pnlUserClock);

            // Center Selected Table Title (Dynamically Centered)
            var pnlCenterBadge = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            lblSelectedTableTitle = new Label
            {
                Text = "📍 Đang chọn: Mang về",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(85, 48, 40),
                Size = new Size(300, 38),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCenterBadge.Resize += (s, e) =>
            {
                lblSelectedTableTitle.Location = new Point((pnlCenterBadge.Width - lblSelectedTableTitle.Width) / 2, (pnlCenterBadge.Height - lblSelectedTableTitle.Height) / 2);
            };
            pnlCenterBadge.Controls.Add(lblSelectedTableTitle);
            pnlHeader.Controls.Add(pnlCenterBadge);

            return pnlHeader;
        }

        private Panel CreateSidebarPanel()
        {
            var pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(30, 16, 11) };

            var navItems = new[]
            {
                ("☕ Bán Hàng (POS)",       "pos"),
                ("🪑 Quản Lý Bàn",           "table"),
                ("📜 Quản Lý Thực Đơn",      "menu"),
                ("🏷️ Quản Lý Danh Mục",      "category"),
                ("🧋 Quản Lý Topping",        "topping"),
                ("📋 Lịch Sử Đơn Hàng",      "history"),
                ("📊 Báo Cáo Doanh Thu",      "analytics"),
                ("📐 Kích Thước Form",       "size")
            };

            int top = 12;
            foreach (var (label, key) in navItems)
            {
                var btn = new Button
                {
                    Text = label,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 210, 200),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(6, top),
                    Size = new Size(208, 46),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    Padding = new Padding(12, 0, 0, 0),
                    Tag = key
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 40, 32);

                string captureKey = key;
                btn.Click += (s, e) =>
                {
                    if (captureKey != "size") SetActiveNav(btn);
                    switch (captureKey)
                    {
                        case "pos":       SwitchView(pnlPosView);           RefreshPosData(); break;
                        case "table":     SwitchView(pnlTableMgmtView);     LoadTableMgmtData(); break;
                        case "menu":      SwitchView(pnlMenuMgmtView);      LoadMenuMgmtData(); break;
                        case "category":  SwitchView(pnlCategoryMgmtView);  LoadCategoryMgmtData(); break;
                        case "topping":   SwitchView(pnlToppingMgmtView);   LoadToppingMgmtData(); break;
                        case "history":   SwitchView(pnlOrderHistoryView);  LoadOrderHistoryData(); break;
                        case "analytics": SwitchView(pnlAnalyticsView);     LoadAnalyticsData(); break;
                        case "size":      OpenFormSizeDialog(); break;
                    }
                };

                _navButtons.Add(btn);
                pnlSidebar.Controls.Add(btn);
                top += 52;
            }

            return pnlSidebar;
        }

        private void OpenFormSizeDialog()
        {
            using var dlg = new FormSizeDialog(this);
            dlg.ShowDialog(this);
        }

        private void SetActiveNav(Button active)
        {
            foreach (var btn in _navButtons)
            {
                btn.BackColor = btn == active ? Color.FromArgb(85, 48, 40) : Color.Transparent;
                btn.ForeColor = btn == active ? Color.White : Color.FromArgb(220, 210, 200);
            }
        }

        private void SwitchView(Panel viewPanel)
        {
            pnlMainContent.Controls.Clear();
            viewPanel.Dock = DockStyle.Fill;
            pnlMainContent.Controls.Add(viewPanel);
            viewPanel.BringToFront();
        }

        // ════════════════════════════════════════════════════════
        #region 1. POS VIEW
        // ════════════════════════════════════════════════════════

        private void InitializePosView()
        {
            pnlPosView = new Panel { Dock = DockStyle.Fill };

            // Left: Table grid
            var pnlTablesContainer = new Panel { Dock = DockStyle.Left, Width = 330, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(252, 250, 246) };
            var pnlTableHead = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(74, 44, 42) };
            pnlTableHead.Controls.Add(new Label { Text = "SƠ ĐỒ BÀN KHÁCH", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            pnlTablesContainer.Controls.Add(pnlTableHead);

            var btnTakeaway = new Button
            {
                Text = "🛒 ĐƠN MANG VỀ (TAKEAWAY)",
                Dock = DockStyle.Top,
                Height = 46,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTakeaway.FlatAppearance.BorderSize = 0;
            btnTakeaway.Click += (s, e) => SelectTakeaway();
            pnlTablesContainer.Controls.Add(btnTakeaway);

            flpTables = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
            pnlTablesContainer.Controls.Add(flpTables);
            flpTables.BringToFront();
            pnlPosView.Controls.Add(pnlTablesContainer);

            // Right: Cart
            pnlPosView.Controls.Add(CreateCartPanel());

            // Center: Menu
            pnlPosView.Controls.Add(CreateMenuCenterPanel());
        }

        private Panel CreateMenuCenterPanel()
        {
            var pnlCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.FromArgb(248, 245, 240) };

            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
            pnlSearch.Controls.Add(new Label { Text = "🔍 Tìm món:", Location = new Point(4, 14), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(54, 32, 30) });
            txtSearchProduct = new TextBox { Location = new Point(100, 9), Width = 320, Font = new Font("Segoe UI", 11) };
            txtSearchProduct.TextChanged += (s, e) => LoadProductsGrid();
            pnlSearch.Controls.Add(txtSearchProduct);
            pnlCenter.Controls.Add(pnlSearch);

            flpCategories = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 4, 0, 4) };
            pnlCenter.Controls.Add(flpCategories);

            flpProducts = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };
            pnlCenter.Controls.Add(flpProducts);
            flpProducts.BringToFront();

            return pnlCenter;
        }

        private Panel CreateCartPanel()
        {
            var pnlCart = new Panel { Dock = DockStyle.Right, Width = 420, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };

            var pnlCartHeader = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(74, 44, 42) };
            pnlCartHeader.Controls.Add(new Label { Text = "🛒 CHI TIẾT ĐƠN HÀNG", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            pnlCart.Controls.Add(pnlCartHeader);

            dgvCart = new DataGridView
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
                RowTemplate = { Height = 44 },
                ReadOnly = true
            };
            dgvCart.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            dgvCart.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 242, 236);
            dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(50, 30, 25);
            dgvCart.EnableHeadersVisualStyles = false;

            dgvCart.Columns.Add("Id", "ID"); dgvCart.Columns["Id"]!.Visible = false;
            dgvCart.Columns.Add("ItemName", "Tên món");
            dgvCart.Columns.Add("Qty", "SL"); dgvCart.Columns["Qty"]!.Width = 45; dgvCart.Columns["Qty"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvCart.Columns.Add("UnitPrice", "Đơn giá"); dgvCart.Columns["UnitPrice"]!.Width = 85; dgvCart.Columns["UnitPrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCart.Columns.Add("Total", "Thành tiền"); dgvCart.Columns["Total"]!.Width = 95; dgvCart.Columns["Total"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCart.CellDoubleClick += DgvCart_CellDoubleClick;

            pnlCart.Controls.Add(dgvCart);
            dgvCart.BringToFront();

            // Cart summary bottom
            var pnlSummary = new Panel { Dock = DockStyle.Bottom, Height = 220, BackColor = Color.FromArgb(250, 248, 244), BorderStyle = BorderStyle.FixedSingle };

            var tblSum = new TableLayoutPanel { Location = new Point(12, 8), Size = new Size(394, 102), ColumnCount = 2, RowCount = 3 };
            tblSum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tblSum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tblSum.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            tblSum.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            tblSum.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tblSum.Controls.Add(new Label { Text = "Tạm tính:", AutoSize = true, Font = new Font("Segoe UI", 9.5f), Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, 0);
            lblCartSubTotal = new Label { Text = "0đ", Anchor = AnchorStyles.Right | AnchorStyles.Top, Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true };
            tblSum.Controls.Add(lblCartSubTotal, 1, 0);

            var pnlDiscRow = new Panel { Dock = DockStyle.Fill };
            pnlDiscRow.Controls.Add(new Label { Text = "Giảm (%):", Location = new Point(0, 4), AutoSize = true, Font = new Font("Segoe UI", 9.5f) });
            numDiscountPct = new NumericUpDown { Location = new Point(72, 2), Width = 60, Minimum = 0, Maximum = 100, Value = 0, Font = new Font("Segoe UI", 9.5f) };
            numDiscountPct.ValueChanged += (s, e) => { if (_currentOrder != null) { _posService.ApplyDiscount(_currentOrder.Id, numDiscountPct.Value); RefreshCartView(); } };
            pnlDiscRow.Controls.Add(numDiscountPct);
            tblSum.Controls.Add(pnlDiscRow, 0, 1);

            lblCartDiscount = new Label { Text = "0đ", Anchor = AnchorStyles.Right | AnchorStyles.Top, Font = new Font("Segoe UI", 10f, FontStyle.Italic), ForeColor = Color.FromArgb(180, 40, 40), AutoSize = true };
            tblSum.Controls.Add(lblCartDiscount, 1, 1);

            tblSum.Controls.Add(new Label { Text = "TỔNG CỘNG:", AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 4, 0, 0) }, 0, 2);
            lblCartTotal = new Label { Text = "0đ", Anchor = AnchorStyles.Right | AnchorStyles.Top, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.FromArgb(192, 38, 38), AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
            tblSum.Controls.Add(lblCartTotal, 1, 2);

            pnlSummary.Controls.Add(tblSum);

            // Action buttons
            var pnlBtns = new TableLayoutPanel { Location = new Point(12, 118), Size = new Size(394, 90), ColumnCount = 3, RowCount = 1 };
            pnlBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
            pnlBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            pnlBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            var btnDeleteCart = MakeActionButton("❌ Xóa Đơn", Color.FromArgb(220, 38, 38));
            btnDeleteCart.Click += BtnDeleteCart_Click;

            var btnPreview = MakeActionButton("📄 Xem In", Color.FromArgb(37, 99, 235));
            btnPreview.Click += (s, e) =>
            {
                if (_currentOrder != null && _currentOrder.Details.Count > 0)
                    new ReceiptPrinter(_currentOrder).PrintPreview();
            };

            var btnCheckout = MakeActionButton("💰 THANH TOÁN", Color.FromArgb(22, 163, 74));
            btnCheckout.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnCheckout.Click += BtnCheckout_Click;

            pnlBtns.Controls.Add(btnDeleteCart, 0, 0);
            pnlBtns.Controls.Add(btnPreview, 1, 0);
            pnlBtns.Controls.Add(btnCheckout, 2, 0);

            pnlSummary.Controls.Add(pnlBtns);
            pnlCart.Controls.Add(pnlSummary);

            return pnlCart;
        }

        private static Button MakeActionButton(string text, Color bg) => new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        private void RefreshPosData()
        {
            LoadTablesGrid();
            LoadCategoriesPills();
            LoadProductsGrid();
            RefreshCartView();
        }

        private void LoadTablesGrid()
        {
            flpTables.Controls.Clear();
            var tables = _posService.GetTables();

            foreach (var tbl in tables)
            {
                var btnTbl = new Button
                {
                    Size = new Size(145, 90),
                    Margin = new Padding(5),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btnTbl.FlatAppearance.BorderSize = 2;

                if (tbl.Status == TableStatus.Occupied)
                {
                    btnTbl.Text = $"🪑 {tbl.Name}\n🔴 CÓ KHÁCH\n{tbl.CurrentTotal:N0}đ";
                    btnTbl.BackColor = Color.FromArgb(254, 243, 199);
                    btnTbl.ForeColor = Color.FromArgb(146, 64, 14);
                    btnTbl.FlatAppearance.BorderColor = Color.FromArgb(217, 119, 6);
                }
                else
                {
                    btnTbl.Text = $"🪑 {tbl.Name}\n🟢 BÀN TRỐNG\n({tbl.Capacity} chỗ)";
                    btnTbl.BackColor = Color.FromArgb(240, 253, 244);
                    btnTbl.ForeColor = Color.FromArgb(22, 101, 52);
                    btnTbl.FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);
                }

                if (_selectedTable != null && _selectedTable.Id == tbl.Id)
                {
                    btnTbl.FlatAppearance.BorderColor = Color.FromArgb(185, 28, 28);
                    btnTbl.FlatAppearance.BorderSize = 3;
                    btnTbl.BackColor = Color.FromArgb(254, 226, 226);
                }

                btnTbl.Click += (s, e) => SelectTable(tbl);
                flpTables.Controls.Add(btnTbl);
            }
        }

        private void SelectTable(DiningTable table)
        {
            _selectedTable = table;
            lblSelectedTableTitle.Text = $"📍 Đang chọn: {table.Name}";
            _currentOrder = _posService.GetOrCreateActiveOrder(table.Id, table.Name);
            LoadTablesGrid();
            RefreshCartView();
        }

        private void SelectTakeaway()
        {
            _selectedTable = null;
            lblSelectedTableTitle.Text = "📍 Đang chọn: Mang về";
            _currentOrder = _posService.GetOrCreateActiveOrder(null, "Mang về");
            LoadTablesGrid();
            RefreshCartView();
        }

        private void LoadCategoriesPills()
        {
            flpCategories.Controls.Clear();
            var categories = _posService.GetCategories();

            var btnAll = new Button
            {
                Text = "☕ Tất Cả",
                Height = 36,
                AutoSize = true,
                Padding = new Padding(10, 0, 10, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = _selectedCategoryId == 0 ? Color.FromArgb(74, 44, 42) : Color.White,
                ForeColor = _selectedCategoryId == 0 ? Color.White : Color.FromArgb(50, 30, 25),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAll.FlatAppearance.BorderColor = Color.FromArgb(200, 190, 180);
            btnAll.Click += (s, e) => { _selectedCategoryId = 0; LoadCategoriesPills(); LoadProductsGrid(); };
            flpCategories.Controls.Add(btnAll);

            foreach (var cat in categories)
            {
                var btnCat = new Button
                {
                    Text = $"{cat.Icon} {cat.Name}",
                    Height = 36,
                    AutoSize = true,
                    Padding = new Padding(10, 0, 10, 0),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = _selectedCategoryId == cat.Id ? Color.FromArgb(74, 44, 42) : Color.White,
                    ForeColor = _selectedCategoryId == cat.Id ? Color.White : Color.FromArgb(50, 30, 25),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnCat.FlatAppearance.BorderColor = Color.FromArgb(200, 190, 180);
                btnCat.Click += (s, e) => { _selectedCategoryId = cat.Id; LoadCategoriesPills(); LoadProductsGrid(); };
                flpCategories.Controls.Add(btnCat);
            }
        }

        private void LoadProductsGrid()
        {
            flpProducts.Controls.Clear();
            var products = _posService.GetProducts(_selectedCategoryId, txtSearchProduct.Text.Trim());

            foreach (var prod in products)
            {
                var pnlCard = new Panel
                {
                    Size = new Size(185, 215),
                    Margin = new Padding(6),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                    Cursor = Cursors.Hand
                };

                var picProd = new PictureBox
                {
                    Location = new Point(0, 0),
                    Size = new Size(183, 105),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(246, 242, 238),
                    Dock = DockStyle.Top
                };

                Image? loadedImg = GetProductImage(prod.ImageUrl);
                if (loadedImg != null)
                {
                    picProd.Image = loadedImg;
                }
                else
                {
                    picProd.Paint += (s, pe) =>
                    {
                        using var f = new Font("Segoe UI", 26);
                        using var b = new SolidBrush(Color.FromArgb(160, 130, 110));
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        pe.Graphics.DrawString("☕", f, b, new RectangleF(0, 0, picProd.Width, picProd.Height), sf);
                    };
                }

                var pnlInfo = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(6, 4, 6, 4),
                    BackColor = Color.White
                };

                var lblName = new Label
                {
                    Text = prod.Name,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(45, 28, 25),
                    Location = new Point(6, 4),
                    Size = new Size(170, 36),
                    TextAlign = ContentAlignment.TopLeft,
                    AutoEllipsis = true
                };

                var lblPrice = new Label
                {
                    Text = $"{prod.Price:N0}đ",
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(192, 38, 38),
                    Location = new Point(6, 42),
                    Size = new Size(95, 28),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };

                var btnAdd = new Button
                {
                    Text = "THÊM +",
                    Location = new Point(104, 42),
                    Size = new Size(68, 28),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(74, 44, 42),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnAdd.FlatAppearance.BorderSize = 0;

                Action triggerAdd = () => AddProductToCart(prod);
                pnlCard.Click += (s, e) => triggerAdd();
                picProd.Click += (s, e) => triggerAdd();
                lblName.Click += (s, e) => triggerAdd();
                lblPrice.Click += (s, e) => triggerAdd();
                btnAdd.Click += (s, e) => triggerAdd();

                pnlInfo.Controls.Add(lblName);
                pnlInfo.Controls.Add(lblPrice);
                pnlInfo.Controls.Add(btnAdd);

                pnlCard.Controls.Add(pnlInfo);
                pnlCard.Controls.Add(picProd);
                pnlInfo.BringToFront();

                flpProducts.Controls.Add(pnlCard);
            }
        }

        private static Image? GetProductImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;

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
                    return Image.FromStream(stream);
                }
                catch { }
            }
            return null;
        }

        private void AddProductToCart(Product product)
        {
            if (_currentOrder == null) SelectTakeaway();

            using var dlg = new CustomizationForm(product, _posService);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var detail = dlg.SelectedDetail;
                detail.OrderId = _currentOrder!.Id;
                _posService.SaveOrderDetailItem(detail);
                RefreshCartView();
                LoadTablesGrid();
            }
        }

        private void RefreshCartView()
        {
            dgvCart.Rows.Clear();
            if (_currentOrder == null) return;

            _currentOrder = _posService.GetOrderById(_currentOrder.Id);
            if (_currentOrder == null) return;

            foreach (var item in _currentOrder.Details)
            {
                string desc = item.ProductName;
                if (!string.IsNullOrWhiteSpace(item.Size))
                    desc += $" ({item.Size}, Đường {item.Sugar}, Đá {item.Ice})";
                if (!string.IsNullOrWhiteSpace(item.Toppings)) desc += $" +{item.Toppings}";
                dgvCart.Rows.Add(item.Id, desc, item.Quantity, $"{item.SingleItemTotal:N0}đ", $"{item.SubTotal:N0}đ");
            }

            lblCartSubTotal.Text = $"{_currentOrder.SubTotal:N0}đ";
            lblCartDiscount.Text = $"-{_currentOrder.DiscountAmount:N0}đ";
            lblCartTotal.Text = $"{_currentOrder.TotalAmount:N0}đ";
            numDiscountPct.Value = _currentOrder.DiscountPercent;
        }

        private void DgvCart_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _currentOrder == null) return;
            int detailId = Convert.ToInt32(dgvCart.Rows[e.RowIndex].Cells["Id"].Value);
            if (MessageBox.Show("Bạn muốn xóa món này khỏi đơn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _posService.RemoveOrderDetailItem(detailId, _currentOrder.Id);
                RefreshCartView(); LoadTablesGrid();
            }
        }

        private void BtnDeleteCart_Click(object? sender, EventArgs e)
        {
            if (_currentOrder == null || _currentOrder.Details.Count == 0) return;
            if (MessageBox.Show("Hủy toàn bộ đơn hàng này?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _posService.CancelOrder(_currentOrder.Id);
                if (_selectedTable != null) SelectTable(_selectedTable);
                else SelectTakeaway();
            }
        }

        private void BtnCheckout_Click(object? sender, EventArgs e)
        {
            if (_currentOrder == null || _currentOrder.Details.Count == 0)
            {
                MessageBox.Show("Đơn hàng đang trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new PaymentForm(_currentOrder, checkoutOrder => _posService.CheckoutOrder(checkoutOrder));
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show("Thanh toán thành công! 🎉", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_selectedTable != null) SelectTable(_selectedTable);
                else SelectTakeaway();
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region 2. TABLE MANAGEMENT VIEW
        // ════════════════════════════════════════════════════════

        private void InitializeTableMgmtView()
        {
            pnlTableMgmtView = new Panel { Dock = DockStyle.Fill };

            var pnlTop = BuildMgmtToolbar("QUẢN LÝ DANH SÁCH BÀN", "➕ Thêm Bàn", out var btnAdd);
            btnAdd.Click += (s, e) =>
            {
                using var dlg = new TableEditForm();
                if (dlg.ShowDialog() == DialogResult.OK)
                { _posService.SaveTable(dlg.EditingTable); LoadTableMgmtData(); }
            };
            pnlTableMgmtView.Controls.Add(pnlTop);

            dgvTableMgmt = BuildMgmtGrid(new[] { ("Id", "Mã", 60), ("Name", "Tên Bàn", 0), ("Capacity", "Sức Chứa", 120), ("Status", "Trạng Thái", 140) });
            pnlTableMgmtView.Controls.Add(dgvTableMgmt);

            dgvTableMgmt.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                int id = Convert.ToInt32(dgvTableMgmt.Rows[e.RowIndex].Cells["Id"].Value);
                string colName = dgvTableMgmt.Columns[e.ColumnIndex].Name;

                if (colName == "EditBtn")
                {
                    var tables = _posService.GetTables();
                    var tbl = tables.Find(t => t.Id == id);
                    if (tbl == null) return;
                    using var dlg = new TableEditForm(tbl);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    { _posService.SaveTable(dlg.EditingTable); LoadTableMgmtData(); }
                }
                else if (colName == "DeleteBtn")
                {
                    if (MessageBox.Show("Xóa bàn này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        if (!_posService.DeleteTable(id))
                            MessageBox.Show("Không thể xóa bàn đang có khách!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        else LoadTableMgmtData();
                    }
                }
            };
        }

        private void LoadTableMgmtData()
        {
            dgvTableMgmt.Rows.Clear();
            foreach (var t in _posService.GetTables())
                dgvTableMgmt.Rows.Add(t.Id, t.Name, $"{t.Capacity} người", t.Status == TableStatus.Occupied ? "🔴 Có khách" : "🟢 Trống");
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region 3. MENU MANAGEMENT VIEW
        // ════════════════════════════════════════════════════════

        private void InitializeMenuMgmtView()
        {
            pnlMenuMgmtView = new Panel { Dock = DockStyle.Fill };

            var pnlTop = BuildMgmtToolbar("QUẢN LÝ THỰC ĐƠN MÓN", "➕ Thêm Món", out var btnAdd);
            btnAdd.Click += (s, e) =>
            {
                var cats = _posService.GetCategories();
                using var dlg = new ProductEditForm(null, cats, _posService);
                if (dlg.ShowDialog() == DialogResult.OK)
                { _posService.SaveProduct(dlg.EditingProduct); LoadMenuMgmtData(); }
            };
            pnlMenuMgmtView.Controls.Add(pnlTop);

            dgvMenuMgmt = BuildMgmtGrid(new[] { ("Id", "Mã", 60), ("ImageInfo", "Hình Ảnh", 95), ("Name", "Tên Món", 200), ("Category", "Danh Mục", 150), ("Price", "Giá Bán", 110), ("Available", "Bán", 70), ("Description", "Mô Tả", 0) });
            pnlMenuMgmtView.Controls.Add(dgvMenuMgmt);

            dgvMenuMgmt.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                int id = Convert.ToInt32(dgvMenuMgmt.Rows[e.RowIndex].Cells["Id"].Value);
                string colName = dgvMenuMgmt.Columns[e.ColumnIndex].Name;

                if (colName == "EditBtn")
                {
                    var prods = _posService.GetProducts(includeUnavailable: true);
                    var prod = prods.Find(p => p.Id == id);
                    if (prod == null) return;
                    var cats = _posService.GetCategories();
                    using var dlg = new ProductEditForm(prod, cats, _posService);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    { _posService.SaveProduct(dlg.EditingProduct); LoadMenuMgmtData(); }
                }
                else if (colName == "DeleteBtn")
                {
                    if (MessageBox.Show("Xóa món này khỏi thực đơn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    { _posService.DeleteProduct(id); LoadMenuMgmtData(); }
                }
            };
        }

        private void LoadMenuMgmtData()
        {
            dgvMenuMgmt.Rows.Clear();
            foreach (var p in _posService.GetProducts(includeUnavailable: true))
            {
                string hasImg = !string.IsNullOrWhiteSpace(p.ImageUrl) ? "📸 Có ảnh" : "❌ Chưa có";
                dgvMenuMgmt.Rows.Add(p.Id, hasImg, p.Name, p.CategoryName, $"{p.Price:N0}đ", p.IsAvailable ? "✅" : "❌", p.Description);
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region 4. CATEGORY MANAGEMENT VIEW
        // ════════════════════════════════════════════════════════

        private void InitializeCategoryMgmtView()
        {
            pnlCategoryMgmtView = new Panel { Dock = DockStyle.Fill };

            var pnlTop = BuildMgmtToolbar("QUẢN LÝ DANH MỤC SẢN PHẨM", "➕ Thêm Danh Mục", out var btnAdd);
            btnAdd.Click += (s, e) =>
            {
                using var dlg = new CategoryEditForm();
                if (dlg.ShowDialog() == DialogResult.OK)
                { _posService.SaveCategory(dlg.EditingCategory); LoadCategoryMgmtData(); }
            };
            pnlCategoryMgmtView.Controls.Add(pnlTop);

            dgvCategoryMgmt = BuildMgmtGrid(new[] { ("Id", "Mã", 60), ("Icon", "Icon", 70), ("Name", "Tên Danh Mục", 0), ("Order", "Thứ Tự", 90) });
            pnlCategoryMgmtView.Controls.Add(dgvCategoryMgmt);

            dgvCategoryMgmt.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                int id = Convert.ToInt32(dgvCategoryMgmt.Rows[e.RowIndex].Cells["Id"].Value);
                string colName = dgvCategoryMgmt.Columns[e.ColumnIndex].Name;

                if (colName == "EditBtn")
                {
                    var cats = _posService.GetCategories();
                    var cat = cats.Find(c => c.Id == id);
                    if (cat == null) return;
                    using var dlg = new CategoryEditForm(cat);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    { _posService.SaveCategory(dlg.EditingCategory); LoadCategoryMgmtData(); }
                }
                else if (colName == "DeleteBtn")
                {
                    if (MessageBox.Show("Xóa danh mục này?\n(Không thể xóa nếu còn sản phẩm trong danh mục)", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        if (!_posService.DeleteCategory(id))
                            MessageBox.Show("Không thể xóa! Danh mục này vẫn còn sản phẩm.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        else LoadCategoryMgmtData();
                    }
                }
            };
        }

        private void LoadCategoryMgmtData()
        {
            dgvCategoryMgmt.Rows.Clear();
            foreach (var c in _posService.GetCategories())
                dgvCategoryMgmt.Rows.Add(c.Id, c.Icon, c.Name, c.DisplayOrder);
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region 5. TOPPING MANAGEMENT VIEW
        // ════════════════════════════════════════════════════════

        private void InitializeToppingMgmtView()
        {
            pnlToppingMgmtView = new Panel { Dock = DockStyle.Fill };

            var pnlTop = BuildMgmtToolbar("QUẢN LÝ TOPPING", "➕ Thêm Topping", out var btnAdd);
            btnAdd.Click += (s, e) =>
            {
                using var dlg = new ToppingEditForm();
                if (dlg.ShowDialog() == DialogResult.OK)
                { _posService.SaveTopping(dlg.EditingTopping); LoadToppingMgmtData(); }
            };
            pnlToppingMgmtView.Controls.Add(pnlTop);

            // Add "Quản lý Size mặc định" button in the right action panel
            var pnlRightActions = pnlTop.Controls["pnlRightActions"] as Panel;
            if (pnlRightActions != null)
            {
                var btnGlobalSizes = new Button
                {
                    Text = "📐 Quản Lý Size Mặc Định",
                    Size = new Size(195, 38),
                    Location = new Point(pnlRightActions.Width - 165 - 205, 0),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btnGlobalSizes.FlatAppearance.BorderSize = 0;
                btnGlobalSizes.Click += (s, e) =>
                {
                    using var szForm = new ProductSizeForm(_posService, 0, "Tất cả món (mặc định)");
                    szForm.ShowDialog();
                };
                pnlRightActions.Controls.Add(btnGlobalSizes);
            }

            dgvToppingMgmt = BuildMgmtGrid(new[] { ("Id", "Mã", 60), ("Name", "Tên Topping", 0), ("Price", "Phụ Thu", 130), ("Available", "Đang Bán", 90) });
            pnlToppingMgmtView.Controls.Add(dgvToppingMgmt);

            dgvToppingMgmt.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                int id = Convert.ToInt32(dgvToppingMgmt.Rows[e.RowIndex].Cells["Id"].Value);
                string colName = dgvToppingMgmt.Columns[e.ColumnIndex].Name;

                if (colName == "EditBtn")
                {
                    var tops = _posService.GetToppings();
                    var top = tops.Find(t => t.Id == id);
                    if (top == null) return;
                    using var dlg = new ToppingEditForm(top);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    { _posService.SaveTopping(dlg.EditingTopping); LoadToppingMgmtData(); }
                }
                else if (colName == "DeleteBtn")
                {
                    if (MessageBox.Show("Xóa topping này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    { _posService.DeleteTopping(id); LoadToppingMgmtData(); }
                }
            };
        }

        private void LoadToppingMgmtData()
        {
            dgvToppingMgmt.Rows.Clear();
            foreach (var t in _posService.GetToppings())
                dgvToppingMgmt.Rows.Add(t.Id, t.Name, $"{t.Price:N0}đ", t.IsAvailable ? "✅" : "❌");
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region 6. ORDER HISTORY VIEW
        // ════════════════════════════════════════════════════════

        private void InitializeOrderHistoryView()
        {
            pnlOrderHistoryView = new Panel { Dock = DockStyle.Fill };
            var pnlTop = BuildMgmtToolbar("LỊCH SỬ ĐƠN HÀNG ĐÃ THANH TOÁN", null, out _);
            pnlOrderHistoryView.Controls.Add(pnlTop);

            dgvOrderHistory = BuildMgmtGrid(new[] { ("Id", "Mã HĐ", 65), ("Code", "Mã Đơn", 150), ("Table", "Vị Trí", 160), ("Date", "Thời Gian", 160), ("Method", "Hình Thức", 160), ("Total", "Tổng Tiền", 130) }, addActionCols: false);
            pnlOrderHistoryView.Controls.Add(dgvOrderHistory);
        }

        private void LoadOrderHistoryData()
        {
            dgvOrderHistory.Rows.Clear();
            foreach (var o in _posService.GetCompletedOrders(DateTime.Today.AddDays(-30), DateTime.Today))
                dgvOrderHistory.Rows.Add(o.Id, o.OrderCode, o.TableName, o.OrderDate.ToString("dd/MM/yyyy HH:mm"), o.PaymentMethod, $"{o.TotalAmount:N0}đ");
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region 7. ANALYTICS VIEW
        // ════════════════════════════════════════════════════════

        private void InitializeAnalyticsView()
        {
            pnlAnalyticsView = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(248, 245, 240) };

            // Master container (auto-sized for scrolling)
            var pnlContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20, 16, 20, 16) };
            pnlAnalyticsView.Controls.Add(pnlContent);

            int y = 8;

            // ── Title ──
            pnlContent.Controls.Add(new Label
            {
                Text = "📊 THỐNG KÊ & BÁO CÁO DOANH THU",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 32, 30),
                Location = new Point(20, y),
                AutoSize = true
            });
            y += 40;

            // ── Section 1: Entity Counts Cards ──
            pnlContent.Controls.Add(new Label
            {
                Text = "TỔNG QUAN HỆ THỐNG",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 80, 60),
                Location = new Point(20, y),
                AutoSize = true
            });
            y += 24;

            var flpEntityCards = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(1100, 80),
                AutoScroll = false,
                WrapContents = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblCountCategories = CreateSmallStatCard(flpEntityCards, "🏷️ Danh Mục", "0", Color.FromArgb(124, 58, 237));
            lblCountProducts = CreateSmallStatCard(flpEntityCards, "📜 Số Món", "0", Color.FromArgb(37, 99, 235));
            lblCountTables = CreateSmallStatCard(flpEntityCards, "🪑 Số Bàn", "0", Color.FromArgb(5, 150, 105));
            lblCountSizes = CreateSmallStatCard(flpEntityCards, "📐 Sizes", "0", Color.FromArgb(217, 119, 6));
            lblCountToppings = CreateSmallStatCard(flpEntityCards, "🧋 Topping", "0", Color.FromArgb(190, 18, 60));

            pnlContent.Controls.Add(flpEntityCards);
            y += 88;

            // ── Section 2: Financial Cards ──
            pnlContent.Controls.Add(new Label
            {
                Text = "CHỈ SỐ TÀI CHÍNH HÔM NAY",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 80, 60),
                Location = new Point(20, y),
                AutoSize = true
            });
            y += 24;

            var flpFinCards = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(1100, 100),
                AutoScroll = false,
                WrapContents = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblAnalyticsRevenue = CreateBigStatCard(flpFinCards, "💰 TỔNG DOANH THU", "0đ", Color.FromArgb(34, 139, 34));
            lblAnalyticsOrders = CreateBigStatCard(flpFinCards, "📋 TỔNG ĐƠN HÀNG", "0 đơn", Color.FromArgb(37, 99, 235));
            lblAnalyticsAvg = CreateBigStatCard(flpFinCards, "💵 GIÁ TRỊ TB / ĐƠN", "0đ", Color.FromArgb(217, 119, 6));

            pnlContent.Controls.Add(flpFinCards);
            y += 108;

            // ── Section 3: Period Filter Buttons ──
            var pnlPeriodFilter = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(600, 44),
                AutoScroll = false,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            pnlPeriodFilter.Controls.Add(new Label
            {
                Text = "BIỂU ĐỒ DOANH THU:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 32, 30),
                AutoSize = true,
                Margin = new Padding(0, 8, 10, 0)
            });

            var btnDay = MakePeriodButton("📅 7 Ngày Qua", "day");
            var btnMonth = MakePeriodButton("📅 12 Tháng", "month");
            var btnYear = MakePeriodButton("📅 5 Năm Qua", "year");

            pnlPeriodFilter.Controls.Add(btnDay);
            pnlPeriodFilter.Controls.Add(btnMonth);
            pnlPeriodFilter.Controls.Add(btnYear);

            pnlContent.Controls.Add(pnlPeriodFilter);
            y += 50;

            // ── Section 4: Revenue Bar Chart ──
            _revenueChart = new RevenueBarChart
            {
                Location = new Point(20, y),
                Size = new Size(1100, 340),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Title = "DOANH THU 7 NGÀY GẦN ĐÂY"
            };
            pnlContent.Controls.Add(_revenueChart);
        }

        private Button MakePeriodButton(string text, string periodKey)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = _currentPeriod == periodKey ? Color.FromArgb(74, 44, 42) : Color.White,
                ForeColor = _currentPeriod == periodKey ? Color.White : Color.FromArgb(50, 30, 20),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 34),
                Margin = new Padding(0, 2, 8, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(200, 190, 180);
            btn.Click += (s, e) =>
            {
                _currentPeriod = periodKey;
                LoadAnalyticsData();
            };
            return btn;
        }

        private static Label CreateSmallStatCard(FlowLayoutPanel parent, string title, string initialVal, Color bg)
        {
            var pnl = new Panel { Size = new Size(190, 68), Margin = new Padding(0, 0, 12, 0), BackColor = bg };
            var lblT = new Label { Text = title, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(240, 240, 240), Location = new Point(12, 8), AutoSize = true };
            var lblV = new Label { Text = initialVal, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(12, 32), Size = new Size(165, 30), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            parent.Controls.Add(pnl);
            return lblV;
        }

        private static Label CreateBigStatCard(FlowLayoutPanel parent, string title, string initialVal, Color bg)
        {
            var pnl = new Panel { Size = new Size(310, 90), Margin = new Padding(0, 0, 16, 0), BackColor = bg };
            var lblT = new Label { Text = title, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(245, 245, 245), Location = new Point(16, 12), AutoSize = true };
            var lblV = new Label { Text = initialVal, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 42), Size = new Size(278, 40), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            parent.Controls.Add(pnl);
            return lblV;
        }

        private void LoadAnalyticsData()
        {
            // Financial stats
            var (rev, count, avg) = _posService.GetTodayStats();
            lblAnalyticsRevenue.Text = $"{rev:N0}đ";
            lblAnalyticsOrders.Text = $"{count} đơn";
            lblAnalyticsAvg.Text = $"{avg:N0}đ";

            // Entity counts
            var (catCount, prodCount, tblCount, szCount, topCount) = _posService.GetEntityCounts();
            lblCountCategories.Text = catCount.ToString();
            lblCountProducts.Text = prodCount.ToString();
            lblCountTables.Text = tblCount.ToString();
            lblCountSizes.Text = szCount.ToString();
            lblCountToppings.Text = topCount.ToString();

            // Bar chart data
            var chartData = _posService.GetRevenueDataByPeriod(_currentPeriod);
            _revenueChart.Items = chartData;

            switch (_currentPeriod)
            {
                case "day":   _revenueChart.Title = "DOANH THU 7 NGÀY GẦN ĐÂY"; break;
                case "month": _revenueChart.Title = "DOANH THU 12 THÁNG TRONG NĂM"; break;
                case "year":  _revenueChart.Title = "DOANH THU 5 NĂM GẦN ĐÂY"; break;
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════
        #region SHARED BUILDER HELPERS
        // ════════════════════════════════════════════════════════

        /// <summary>Builds the standard top toolbar for management views with dock-responsive alignment.</summary>
        private static Panel BuildMgmtToolbar(string title, string? addBtnText, out Button addBtn)
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(248, 245, 240), Padding = new Padding(16, 10, 16, 10) };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 32, 30),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            pnlTop.Controls.Add(lblTitle);

            var pnlRightActions = new Panel { Dock = DockStyle.Right, Width = 450, BackColor = Color.Transparent, Name = "pnlRightActions" };
            pnlTop.Controls.Add(pnlRightActions);

            addBtn = new Button();
            if (!string.IsNullOrEmpty(addBtnText))
            {
                addBtn.Text = addBtnText;
                addBtn.Size = new Size(160, 38);
                addBtn.Location = new Point(450 - 160, 0);
                addBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                addBtn.BackColor = Color.FromArgb(46, 125, 50);
                addBtn.ForeColor = Color.White;
                addBtn.FlatStyle = FlatStyle.Flat;
                addBtn.FlatAppearance.BorderSize = 0;
                addBtn.Cursor = Cursors.Hand;
                addBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                pnlRightActions.Controls.Add(addBtn);
            }

            return pnlTop;
        }

        /// <summary>Builds a standard management DataGridView with Sửa/Xóa action columns.</summary>
        private static DataGridView BuildMgmtGrid((string Name, string Header, int Width)[] columns, bool addActionCols = true)
        {
            var dgv = new DataGridView
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
                RowTemplate = { Height = 42 },
                ReadOnly = true
            };
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(74, 44, 42);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;

            foreach (var (name, header, width) in columns)
            {
                dgv.Columns.Add(name, header);
                if (width > 0) { dgv.Columns[name]!.Width = width; dgv.Columns[name]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; }
            }

            if (addActionCols)
            {
                var btnEdit = new DataGridViewButtonColumn { Name = "EditBtn", HeaderText = "", Text = "✏️ Sửa", UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, Width = 85, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
                dgv.Columns.Add(btnEdit);

                var btnDel = new DataGridViewButtonColumn { Name = "DeleteBtn", HeaderText = "", Text = "🗑 Xóa", UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, Width = 85, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
                dgv.Columns.Add(btnDel);
            }

            return dgv;
        }

        #endregion
    }
}

