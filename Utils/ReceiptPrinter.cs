using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using TommyPOS.Models;

namespace TommyPOS.Utils
{
    public class ReceiptPrinter
    {
        private readonly Order _order;
        private readonly Image? _logoImage;

        public ReceiptPrinter(Order order)
        {
            _order = order;
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
            if (!File.Exists(logoPath))
            {
                logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.png");
            }

            if (File.Exists(logoPath))
            {
                try { _logoImage = Image.FromFile(logoPath); } catch { }
            }
        }

        public void PrintPreview()
        {
            var printDoc = new PrintDocument();
            // Standard 80mm POS paper: width 285 units (approx 80mm @ 96DPI)
            printDoc.DefaultPageSettings.PaperSize = new PaperSize("POS-80mm", 285, 1200);
            printDoc.PrintPage += PrintPageHandler;

            var printPreviewDialog = new PrintPreviewDialog
            {
                Document = printDoc,
                Width = 520,
                Height = 750,
                StartPosition = FormStartPosition.CenterScreen,
                Text = $"Xem Trước Hóa Đơn 80mm - {_order.OrderCode}"
            };

            printPreviewDialog.ShowDialog();
        }

        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics ?? throw new InvalidOperationException();
            int width = 275; // 80mm printable boundary
            float y = 12;

            var fontTitle = new Font("Segoe UI", 13, FontStyle.Bold);
            var fontSubTitle = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            var fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var fontRegular = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            var fontItalic = new Font("Segoe UI", 8f, FontStyle.Italic);
            var fontSmall = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            var fontTotal = new Font("Segoe UI", 12, FontStyle.Bold);

            var brush = Brushes.Black;
            var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far };
            var leftFormat = new StringFormat { Alignment = StringAlignment.Near };

            // 1. Logo Header
            if (_logoImage != null)
            {
                int logoSize = 60;
                g.DrawImage(_logoImage, (width - logoSize) / 2, y, logoSize, logoSize);
                y += logoSize + 6;
            }

            // 2. Shop Information
            g.DrawString("TOMMY COFFEE & TEA", fontTitle, brush, new RectangleF(0, y, width, 22), centerFormat);
            y += 24;
            g.DrawString("Đ/c: 123 Đường Cà Phê, Q.1, TP. Hồ Chí Minh", fontSubTitle, brush, new RectangleF(0, y, width, 16), centerFormat);
            y += 16;
            g.DrawString("Hotline: 0909 123 456 - Wifi: TommyCoffee (88888888)", fontSubTitle, brush, new RectangleF(0, y, width, 16), centerFormat);
            y += 18;

            // Header Double Line
            g.DrawString("==========================================", fontRegular, brush, 0, y);
            y += 14;

            // 3. Receipt Title & Order Info
            g.DrawString("HÓA ĐƠN THANH TOÁN", fontTitle, brush, new RectangleF(0, y, width, 22), centerFormat);
            y += 22;
            g.DrawString($"(Khổ giấy in nhiệt K80 - 80mm)", fontItalic, brush, new RectangleF(0, y, width, 14), centerFormat);
            y += 18;

            g.DrawString($"Mã HD: {_order.OrderCode}", fontBold, brush, 5, y); y += 16;
            g.DrawString($"Ngày lập: {_order.OrderDate:dd/MM/yyyy HH:mm:ss}", fontRegular, brush, 5, y); y += 16;
            g.DrawString($"Vị trí: {_order.TableName}", fontBold, brush, 5, y); y += 16;
            g.DrawString($"Thu ngân: {_order.CashierName}", fontRegular, brush, 5, y); y += 18;

            // 4. Items Table Header
            g.DrawString("--------------------------------------------------", fontRegular, brush, 0, y);
            y += 13;

            g.DrawString("Món / Tùy chọn", fontBold, brush, 5, y);
            g.DrawString("SL", fontBold, brush, 155, y);
            g.DrawString("Đ.Giá", fontBold, brush, 185, y);
            g.DrawString("T.Tiền", fontBold, brush, new RectangleF(215, y, 55, 15), rightFormat);
            y += 16;

            g.DrawString("--------------------------------------------------", fontRegular, brush, 0, y);
            y += 13;

            // 5. Item Rows
            foreach (var item in _order.Details)
            {
                // Item Name
                g.DrawString(item.ProductName, fontBold, brush, new RectangleF(5, y, 148, 32), leftFormat);
                g.DrawString(item.Quantity.ToString(), fontRegular, brush, 158, y);
                g.DrawString($"{item.UnitPrice:N0}", fontRegular, brush, 180, y);
                g.DrawString($"{item.SubTotal:N0}", fontBold, brush, new RectangleF(215, y, 55, 15), rightFormat);
                y += 18;

                // Options (Size, Sugar, Ice)
                string opts = $"▪ Size: {item.Size} | Đường: {item.Sugar} | Đá: {item.Ice}";
                g.DrawString(opts, fontItalic, brush, 12, y);
                y += 14;

                if (!string.IsNullOrWhiteSpace(item.Toppings))
                {
                    g.DrawString($"▪ Topping: {item.Toppings}", fontItalic, brush, 12, y);
                    y += 14;
                }

                if (!string.IsNullOrWhiteSpace(item.Note))
                {
                    g.DrawString($"▪ Ghi chú: {item.Note}", fontItalic, brush, 12, y);
                    y += 14;
                }

                y += 3;
            }

            g.DrawString("--------------------------------------------------", fontRegular, brush, 0, y);
            y += 14;

            // 6. Financial Summary
            g.DrawString("Tạm tính:", fontRegular, brush, 5, y);
            g.DrawString($"{_order.SubTotal:N0}đ", fontRegular, brush, new RectangleF(150, y, 120, 16), rightFormat);
            y += 18;

            if (_order.DiscountAmount > 0)
            {
                g.DrawString($"Giảm giá ({_order.DiscountPercent}%):", fontRegular, brush, 5, y);
                g.DrawString($"-{_order.DiscountAmount:N0}đ", fontRegular, brush, new RectangleF(150, y, 120, 16), rightFormat);
                y += 18;
            }

            g.DrawString("TỔNG CỘNG:", fontTotal, brush, 5, y);
            g.DrawString($"{_order.TotalAmount:N0}đ", fontTotal, brush, new RectangleF(130, y, 140, 22), rightFormat);
            y += 24;

            g.DrawString("Phương thức TT:", fontRegular, brush, 5, y);
            g.DrawString(_order.PaymentMethod, fontBold, brush, new RectangleF(150, y, 120, 16), rightFormat);
            y += 18;

            if (_order.PaymentMethod == "Tiền mặt")
            {
                g.DrawString("Tiền khách đưa:", fontRegular, brush, 5, y);
                g.DrawString($"{_order.CashGiven:N0}đ", fontRegular, brush, new RectangleF(150, y, 120, 16), rightFormat);
                y += 18;

                g.DrawString("Tiền thối lại:", fontBold, brush, 5, y);
                g.DrawString($"{_order.ChangeAmount:N0}đ", fontBold, brush, new RectangleF(150, y, 120, 16), rightFormat);
                y += 20;
            }

            g.DrawString("==========================================", fontRegular, brush, 0, y);
            y += 16;

            // 7. QR Code Section for Order Lookup / Payment
            try
            {
                string qrData = $"TOMMY_POS|HDO:{_order.OrderCode}|TOTAL:{_order.TotalAmount:0}";
                using Bitmap qrBitmap = QrCodeGenerator.GenerateQrBitmap(qrData, 100, 100);
                g.DrawImage(qrBitmap, (width - 90) / 2, y, 90, 90);
                y += 94;
                g.DrawString("Quét mã QR tra cứu / thanh toán", fontSmall, brush, new RectangleF(0, y, width, 14), centerFormat);
                y += 16;
            }
            catch { }

            // 8. Footer Note
            g.DrawString("Cảm ơn & Hẹn gặp lại quý khách!", fontBold, brush, new RectangleF(0, y, width, 16), centerFormat);
            y += 18;
            g.DrawString("Tommy Coffee - Trao Vị Đậm Đà!", fontItalic, brush, new RectangleF(0, y, width, 16), centerFormat);
            y += 20;

            // Cut line indicator
            g.DrawString("- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -", fontSmall, brush, 0, y);
        }
    }
}
