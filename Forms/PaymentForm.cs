using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using TommyPOS.Models;
using TommyPOS.Utils;

namespace TommyPOS.Forms
{
    public class PaymentForm : Form
    {
        private readonly Order _order;
        private readonly Action<Order> _onCheckoutCompleted;

        private RadioButton rdoCash = null!;
        private RadioButton rdoTransfer = null!;
        private RadioButton rdoEWallet = null!;

        private Panel pnlCashDetails = null!;
        private Panel pnlTransferDetails = null!;
        private Panel pnlEWalletDetails = null!;

        private TextBox txtDiscountPct = null!;
        private Label lblSubTotal = null!;
        private Label lblDiscountAmt = null!;
        private Label lblTotalAmount = null!;

        private TextBox txtCashGiven = null!;
        private Label lblChangeAmt = null!;

        private PictureBox picVietQR = null!;
        private Label lblQRStatus = null!;

        private PictureBox picMoMoQR = null!;
        private Label lblMoMoStatus = null!;

        public bool ShouldPrintReceipt { get; private set; } = true;

        // VietQR Sandbox config – dùng tài khoản demo công khai
        private const string VietQRApiUrl = "https://api.vietqr.io/v2/generate";
        private const string BankBin = "970422";          // MB Bank BIN
        private const string AccountNo = "0987654321";    // STK demo
        private const string AccountName = "TOMMY COFFEE & TEA";
        private const string VietQRClientId = "de1e6e91-2df4-4c02-94c0-00000000000";
        private const string VietQRApiKey   = "sandbox-key-placeholder";

        public PaymentForm(Order order, Action<Order> onCheckoutCompleted)
        {
            _order = order;
            _onCheckoutCompleted = onCheckoutCompleted;
            InitializeUI();
            Recalculate();
        }

        private void InitializeUI()
        {
            Text = $"Thanh Toán Hóa Đơn – {_order.OrderCode}";
            Size = new Size(660, 740);
            MinimumSize = new Size(620, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(248, 245, 240);

            // ── Header ──────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(43, 24, 16) };
            var lblTitle = new Label
            {
                Text = $"THANH TOÁN {_order.TableName.ToUpper()} ({_order.OrderCode})",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 200, 140),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitle);
            Controls.Add(pnlHeader);

            // ── Scrollable body ──────────────────────────────
            var pnlScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16, 12, 16, 12) };
            Controls.Add(pnlScroll);

            int y = 10;

            // ── 1. Tổng quan hóa đơn ────────────────────────
            var grpSummary = new GroupBox
            {
                Text = "Tổng Quan Hóa Đơn",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 32, 30),
                Location = new Point(12, y),
                Size = new Size(612, 135),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var tblSum = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(12, 22, 12, 8) };
            tblSum.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            tblSum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            tblSum.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            tblSum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            tblSum.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            tblSum.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblSubLbl = new Label { Text = "Tạm tính:", Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9.5f), AutoSize = true };
            lblSubTotal = new Label { Text = $"{_order.SubTotal:N0}đ", Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true };
            var lblDiscLbl = new Label { Text = "Giảm giá (%):", Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9.5f), AutoSize = true };
            var pnlDisc = new Panel { Dock = DockStyle.Fill };
            txtDiscountPct = new TextBox { Text = _order.DiscountPercent.ToString("0"), Location = new Point(0, 4), Width = 60, Font = new Font("Segoe UI", 10) };
            lblDiscountAmt = new Label { Text = $"-{_order.DiscountAmount:N0}đ", Location = new Point(68, 6), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Italic), ForeColor = Color.DarkRed };
            pnlDisc.Controls.Add(txtDiscountPct);
            pnlDisc.Controls.Add(lblDiscountAmt);
            txtDiscountPct.TextChanged += (s, e) => Recalculate();

            var lblTotLbl = new Label { Text = "THÀNH TIỀN:", Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), AutoSize = true };
            lblTotalAmount = new Label
            {
                Text = $"{_order.TotalAmount:N0}đ",
                Anchor = AnchorStyles.Left,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 38, 38),
                AutoSize = true
            };

            tblSum.Controls.Add(lblSubLbl, 0, 0);
            tblSum.Controls.Add(lblSubTotal, 1, 0);
            tblSum.Controls.Add(lblDiscLbl, 2, 0);
            tblSum.Controls.Add(pnlDisc, 3, 0);
            tblSum.Controls.Add(lblTotLbl, 0, 1);
            tblSum.SetColumnSpan(lblTotalAmount, 3);
            tblSum.Controls.Add(lblTotalAmount, 1, 1);

            grpSummary.Controls.Add(tblSum);
            pnlScroll.Controls.Add(grpSummary);
            y += grpSummary.Height + 12;

            // ── 2. Phương thức thanh toán ────────────────────
            var grpMethod = new GroupBox
            {
                Text = "Phương Thức Thanh Toán",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 32, 30),
                Location = new Point(12, y),
                Size = new Size(612, 85),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var tblMethod = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(12, 16, 12, 8) };
            tblMethod.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            tblMethod.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            tblMethod.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

            rdoCash = new RadioButton { Text = "💵 Tiền Mặt", AutoSize = true, Checked = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            rdoTransfer = new RadioButton { Text = "🏦 VietQR", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            rdoEWallet = new RadioButton { Text = "📱 Ví Điện Tử", AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };

            rdoCash.CheckedChanged += PaymentMethod_Changed;
            rdoTransfer.CheckedChanged += PaymentMethod_Changed;
            rdoEWallet.CheckedChanged += PaymentMethod_Changed;

            tblMethod.Controls.Add(rdoCash, 0, 0);
            tblMethod.Controls.Add(rdoTransfer, 1, 0);
            tblMethod.Controls.Add(rdoEWallet, 2, 0);
            grpMethod.Controls.Add(tblMethod);
            pnlScroll.Controls.Add(grpMethod);
            y += grpMethod.Height + 12;

            // ── 3a. Panel Tiền Mặt ───────────────────────────
            pnlCashDetails = BuildCashPanel(y, 612);
            pnlScroll.Controls.Add(pnlCashDetails);

            // ── 3b. Panel VietQR ─────────────────────────────
            pnlTransferDetails = BuildVietQRPanel(y, 612);
            pnlTransferDetails.Visible = false;
            pnlScroll.Controls.Add(pnlTransferDetails);

            // ── 3c. Panel Ví điện tử ─────────────────────────
            pnlEWalletDetails = BuildEWalletPanel(y, 612);
            pnlEWalletDetails.Visible = false;
            pnlScroll.Controls.Add(pnlEWalletDetails);

            y += 245;

            // ── 4. Nút thanh toán ────────────────────────────
            var pnlBtns = new TableLayoutPanel
            {
                Location = new Point(12, y),
                Size = new Size(612, 54),
                ColumnCount = 2,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            pnlBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

            var btnCheckoutOnly = new Button
            {
                Text = "✔ THANH TOÁN (KHÔNG IN)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(210, 205, 195),
                ForeColor = Color.FromArgb(40, 30, 25),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnCheckoutOnly.FlatAppearance.BorderSize = 0;
            btnCheckoutOnly.Click += (s, e) => CompleteCheckout(false);

            var btnCheckoutPrint = new Button
            {
                Text = "🖨️ THANH TOÁN & IN HÓA ĐƠN",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 44, 42),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnCheckoutPrint.FlatAppearance.BorderSize = 0;
            btnCheckoutPrint.Click += (s, e) => CompleteCheckout(true);

            pnlBtns.Controls.Add(btnCheckoutOnly, 0, 0);
            pnlBtns.Controls.Add(btnCheckoutPrint, 1, 0);
            pnlScroll.Controls.Add(pnlBtns);
        }

        // ─────────────────────────────────────────────────────────────────────
        private Panel BuildCashPanel(int top, int width)
        {
            var pnl = new Panel
            {
                Location = new Point(12, top),
                Size = new Size(width, 235),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblCashG = new Label { Text = "Tiền khách đưa (VNĐ):", Location = new Point(16, 18), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
            txtCashGiven = new TextBox { Text = "0", Location = new Point(210, 13), Width = 190, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            txtCashGiven.TextChanged += (s, e) => RecalculateChange();
            pnl.Controls.Add(lblCashG);
            pnl.Controls.Add(txtCashGiven);

            // Quick cash buttons (2 rows)
            long[] quickAmounts = { 10000, 20000, 50000, 100000, 200000, 500000 };
            int bx = 16, by = 58;
            int col = 0;
            foreach (var amt in quickAmounts)
            {
                var btnAmt = new Button
                {
                    Text = $"{amt / 1000}k",
                    Location = new Point(bx, by),
                    Size = new Size(92, 38),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(240, 235, 225),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnAmt.FlatAppearance.BorderSize = 0;
                long captureAmt = amt;
                btnAmt.Click += (s, e) => { txtCashGiven.Text = captureAmt.ToString(); };
                pnl.Controls.Add(btnAmt);
                col++;
                bx += 100;
                if (col % 3 == 0) { bx = 16; by += 46; }
            }

            var btnExact = new Button
            {
                Text = "✅ Khách Đưa Đủ Tiền",
                Location = new Point(320, 58),
                Size = new Size(180, 84),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 245, 220),
                ForeColor = Color.FromArgb(20, 90, 20),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExact.FlatAppearance.BorderSize = 1;
            btnExact.FlatAppearance.BorderColor = Color.FromArgb(100, 180, 100);
            btnExact.Click += (s, e) => { txtCashGiven.Text = _order.TotalAmount.ToString("0"); };
            pnl.Controls.Add(btnExact);

            var lblChg = new Label { Text = "TIỀN THỐI LẠI:", Location = new Point(16, 192), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            lblChangeAmt = new Label
            {
                Text = "0đ",
                Location = new Point(180, 188),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            pnl.Controls.Add(lblChg);
            pnl.Controls.Add(lblChangeAmt);

            return pnl;
        }

        private Panel BuildVietQRPanel(int top, int width)
        {
            var pnl = new Panel
            {
                Location = new Point(12, top),
                Size = new Size(width, 235),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblInfo = new Label
            {
                Text = $"🏦 QUÉT MÃ VIETQR ĐỂ THANH TOÁN\n" +
                       $"Ngân hàng: MB Bank (Quân Đội)\n" +
                       $"STK: {VietQRHelper.DefaultAccountNo}\n" +
                       $"Chủ TK: {VietQRHelper.DefaultAccountName}\n" +
                       $"Nội dung: {_order.OrderCode}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 30, 80),
                Location = new Point(12, 12),
                Size = new Size(340, 130),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(lblInfo);

            picVietQR = new PictureBox
            {
                Location = new Point(390, 10),
                Size = new Size(205, 205),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnl.Controls.Add(picVietQR);

            lblQRStatus = new Label
            {
                Text = "⏳ Đang tạo mã QR...",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Location = new Point(12, 150),
                Size = new Size(340, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(lblQRStatus);

            var btnRefresh = new Button
            {
                Text = "🔄 Tạo lại QR",
                Location = new Point(12, 182),
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 110, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => _ = GenerateVietQRAsync();
            pnl.Controls.Add(btnRefresh);

            return pnl;
        }

        private Panel BuildEWalletPanel(int top, int width)
        {
            var pnl = new Panel
            {
                Location = new Point(12, top),
                Size = new Size(width, 235),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblInfo = new Label
            {
                Text = "📱 THANH TOÁN QUA VÍ ĐIỆN TỬ\n\n" +
                       "• MoMo – Quét QR bằng app MoMo\n" +
                       "• ZaloPay – Quét QR bằng app ZaloPay\n" +
                       "• VNPay – QR thống nhất liên ngân hàng\n\n" +
                       $"Số tiền: {_order.TotalAmount:N0}đ\n" +
                       $"Mã đơn: {_order.OrderCode}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 30, 80),
                Location = new Point(12, 12),
                Size = new Size(340, 175),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(lblInfo);

            picMoMoQR = new PictureBox
            {
                Location = new Point(390, 10),
                Size = new Size(205, 205),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            
            // Generate real MoMo / VietQR payload scanmable by MoMo / ZaloPay / VNPay
            string momoPayload = VietQRHelper.BuildEMVCoPayload(VietQRHelper.DefaultBankBin, VietQRHelper.DefaultAccountNo, _order.TotalAmount, _order.OrderCode);
            picMoMoQR.Image = QrCodeGenerator.GenerateQrBitmap(momoPayload, 205, 205);
            pnl.Controls.Add(picMoMoQR);

            lblMoMoStatus = new Label
            {
                Text = "📱 Quét bằng app MoMo / ZaloPay / VNPay / App Ngân Hàng",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 0, 120),
                Location = new Point(12, 198),
                AutoSize = true
            };
            pnl.Controls.Add(lblMoMoStatus);

            return pnl;
        }

        // ─────────────────────────────────────────────────────────────────────
        private async Task GenerateVietQRAsync()
        {
            lblQRStatus.Text = "⏳ Đang tạo mã VietQR...";
            lblQRStatus.ForeColor = Color.DimGray;
            picVietQR.Image = null;

            try
            {
                string bankBin = VietQRHelper.DefaultBankBin;
                string accountNo = VietQRHelper.DefaultAccountNo;
                string accountName = VietQRHelper.DefaultAccountName;

                // 1. Fetch real online VietQR PNG from VietQR.io (100% official scanmable EMVCo QR)
                var onlineBmp = await VietQRHelper.FetchVietQRImageAsync(bankBin, accountNo, accountName, _order.TotalAmount, _order.OrderCode);
                if (onlineBmp != null)
                {
                    picVietQR.Image = onlineBmp;
                    lblQRStatus.Text = "✅ App MB Bank / Vietcombank / MoMo / ZaloPay quét mã này!";
                    lblQRStatus.ForeColor = Color.DarkGreen;
                    return;
                }
            }
            catch (Exception ex)
            {
                lblQRStatus.Text = $"⚠️ Lỗi kết nối VietQR: {ex.Message}";
            }

            // 2. Offline Fallback: Build real EMVCo payload string & render valid QR matrix
            string emvPayload = VietQRHelper.BuildEMVCoPayload(VietQRHelper.DefaultBankBin, VietQRHelper.DefaultAccountNo, _order.TotalAmount, _order.OrderCode);
            picVietQR.Image = QrCodeGenerator.GenerateQrBitmap(emvPayload, 205, 205);
            lblQRStatus.Text = "⚡ Quét bằng App Ngân Hàng / MoMo (Mã QR EMVCo Chuẩn)";
            lblQRStatus.ForeColor = Color.DarkBlue;
        }

        // ─────────────────────────────────────────────────────────────────────
        private void PaymentMethod_Changed(object? sender, EventArgs e)
        {
            pnlCashDetails.Visible = rdoCash.Checked;
            pnlTransferDetails.Visible = rdoTransfer.Checked;
            pnlEWalletDetails.Visible = rdoEWallet.Checked;

            if (rdoTransfer.Checked)
                _ = GenerateVietQRAsync();
        }

        private void Recalculate()
        {
            if (txtDiscountPct == null) return;

            if (decimal.TryParse(txtDiscountPct.Text.Trim(), out decimal discPct))
            {
                discPct = Math.Max(0, Math.Min(100, discPct));
                _order.DiscountPercent = discPct;
            }
            else _order.DiscountPercent = 0;

            _order.DiscountAmount = _order.SubTotal * (_order.DiscountPercent / 100m);
            _order.TotalAmount = _order.SubTotal - _order.DiscountAmount;

            lblSubTotal.Text = $"{_order.SubTotal:N0}đ";
            lblDiscountAmt.Text = $"-{_order.DiscountAmount:N0}đ";
            lblTotalAmount.Text = $"{_order.TotalAmount:N0}đ";

            if (txtCashGiven != null && (txtCashGiven.Text == "0" || string.IsNullOrWhiteSpace(txtCashGiven.Text)))
                txtCashGiven.Text = _order.TotalAmount.ToString("0");

            RecalculateChange();
        }

        private void RecalculateChange()
        {
            if (txtCashGiven == null || lblChangeAmt == null) return;
            if (decimal.TryParse(txtCashGiven.Text.Trim(), out decimal cash))
            {
                _order.CashGiven = cash;
                _order.ChangeAmount = Math.Max(0, cash - _order.TotalAmount);
            }
            else { _order.CashGiven = 0; _order.ChangeAmount = 0; }
            lblChangeAmt.Text = $"{_order.ChangeAmount:N0}đ";
        }

        private void CompleteCheckout(bool printReceipt)
        {
            ShouldPrintReceipt = printReceipt;

            if (rdoCash.Checked)
            {
                _order.PaymentMethod = "Tiền mặt";
                if (_order.CashGiven < _order.TotalAmount)
                {
                    MessageBox.Show("Số tiền khách đưa chưa đủ để thanh toán!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (rdoTransfer.Checked)
            {
                _order.PaymentMethod = "Chuyển khoản (VietQR)";
                _order.CashGiven = _order.TotalAmount;
                _order.ChangeAmount = 0;
            }
            else
            {
                _order.PaymentMethod = "Ví điện tử";
                _order.CashGiven = _order.TotalAmount;
                _order.ChangeAmount = 0;
            }

            _order.Status = PaymentStatus.Paid;
            _onCheckoutCompleted(_order);

            if (ShouldPrintReceipt)
            {
                var printer = new ReceiptPrinter(_order);
                printer.PrintPreview();
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
