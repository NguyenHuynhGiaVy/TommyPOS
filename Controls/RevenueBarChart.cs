using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TommyPOS.Services;

namespace TommyPOS.Controls
{
    public class RevenueBarChart : Control
    {
        private List<RevenueChartItem> _items = new();
        private string _title = "BIỂU ĐỒ DOANH THU";
        private int _hoveredIndex = -1;

        public List<RevenueChartItem> Items
        {
            get => _items;
            set
            {
                _items = value ?? new List<RevenueChartItem>();
                _hoveredIndex = -1;
                Invalidate();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? "";
                Invalidate();
            }
        }

        public RevenueBarChart()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_items.Count == 0) return;

            int leftMargin = 70;
            int rightMargin = 30;
            int topMargin = 50;
            int bottomMargin = 45;

            float chartWidth = Width - leftMargin - rightMargin;
            float chartHeight = Height - topMargin - bottomMargin;

            if (chartWidth <= 0 || chartHeight <= 0) return;

            float stepX = chartWidth / _items.Count;
            float barWidth = Math.Min(stepX * 0.6f, 65f);

            int newHovered = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                float x = leftMargin + i * stepX + (stepX - barWidth) / 2;
                if (e.X >= x && e.X <= x + barWidth && e.Y >= topMargin && e.Y <= Height - bottomMargin)
                {
                    newHovered = i;
                    break;
                }
            }

            if (newHovered != _hoveredIndex)
            {
                _hoveredIndex = newHovered;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background Card border
            using (var penBorder = new Pen(Color.FromArgb(225, 215, 205), 1))
            {
                g.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
            }

            // Title Banner
            using (var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var brushTitle = new SolidBrush(Color.FromArgb(43, 24, 16)))
            {
                g.DrawString(_title.ToUpper(), fontTitle, brushTitle, 16, 14);
            }

            // Legend / Badge
            string legend = "█ Doanh Thu (VNĐ)   |   Cột cao đại diện tỷ lệ doanh thu";
            using (var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Italic))
            using (var brushSub = new SolidBrush(Color.FromArgb(120, 100, 85)))
            {
                var sfRight = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(legend, fontSub, brushSub, Width - 16, 16, sfRight);
            }

            int leftMargin = 70;
            int rightMargin = 30;
            int topMargin = 55;
            int bottomMargin = 45;

            float chartWidth = Width - leftMargin - rightMargin;
            float chartHeight = Height - topMargin - bottomMargin;

            if (chartWidth <= 50 || chartHeight <= 50) return;

            // Calculate Max Revenue
            decimal maxRev = 100000m; // Minimum 100k
            foreach (var item in _items)
            {
                if (item.Revenue > maxRev) maxRev = item.Revenue;
            }
            // Round maxRev up to nice number
            maxRev = Math.Ceiling(maxRev / 100000m) * 100000m;

            // Draw Y Grid lines (4 steps)
            int yGridCount = 4;
            using var penGrid = new Pen(Color.FromArgb(235, 230, 222), 1) { DashStyle = DashStyle.Dash };
            using var fontY = new Font("Segoe UI", 8f);
            using var brushY = new SolidBrush(Color.FromArgb(120, 100, 85));
            var sfY = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            for (int i = 0; i <= yGridCount; i++)
            {
                float yRatio = (float)i / yGridCount;
                float yPos = Height - bottomMargin - (yRatio * chartHeight);
                decimal val = (decimal)yRatio * maxRev;

                g.DrawLine(penGrid, leftMargin, yPos, Width - rightMargin, yPos);
                string yLabel = val >= 1000000 ? $"{val / 1000000:0.#}M" : (val >= 1000 ? $"{val / 1000:0}k" : "0đ");
                g.DrawString(yLabel, fontY, brushY, leftMargin - 8, yPos, sfY);
            }

            if (_items.Count == 0)
            {
                using var fontEmpty = new Font("Segoe UI", 11f, FontStyle.Italic);
                using var brushEmpty = new SolidBrush(Color.Gray);
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Chưa có dữ liệu thống kê trong khoảng thời gian này", fontEmpty, brushEmpty, new RectangleF(leftMargin, topMargin, chartWidth, chartHeight), sfCenter);
                return;
            }

            // Draw Bars
            float stepX = chartWidth / _items.Count;
            float barWidth = Math.Min(stepX * 0.55f, 65f);

            using var fontVal = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var fontOrder = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            using var fontX = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var brushX = new SolidBrush(Color.FromArgb(50, 30, 20));

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                float x = leftMargin + i * stepX + (stepX - barWidth) / 2;
                float barHeightRatio = (float)(item.Revenue / maxRev);
                float barH = Math.Max(barHeightRatio * chartHeight, 4f); // Minimum 4px for visibility
                float y = Height - bottomMargin - barH;

                bool isHovered = i == _hoveredIndex;

                Color c1 = isHovered ? Color.FromArgb(234, 88, 12) : Color.FromArgb(180, 83, 9);
                Color c2 = isHovered ? Color.FromArgb(194, 65, 12) : Color.FromArgb(120, 53, 15);

                using (var gradBrush = new LinearGradientBrush(new RectangleF(x, y, barWidth, barH), c1, c2, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(gradBrush, x, y, barWidth, barH);
                }

                using (var penBar = new Pen(isHovered ? Color.FromArgb(154, 52, 18) : Color.FromArgb(90, 38, 10), 1))
                {
                    g.DrawRectangle(penBar, x, y, barWidth, barH);
                }

                // Value Text on top of bar
                string valStr = item.Revenue >= 1000000 ? $"{item.Revenue / 1000000:0.##}M" : (item.Revenue > 0 ? $"{item.Revenue:N0}đ" : "0đ");
                using (var brushText = new SolidBrush(isHovered ? Color.FromArgb(194, 65, 12) : Color.FromArgb(60, 35, 20)))
                {
                    var sfVal = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
                    g.DrawString(valStr, fontVal, brushText, x + barWidth / 2, y - 4, sfVal);
                }

                // Order Count badge
                if (item.OrderCount > 0)
                {
                    using var brushOrd = new SolidBrush(Color.FromArgb(100, 80, 70));
                    var sfOrd = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
                    g.DrawString($"({item.OrderCount} đơn)", fontOrder, brushOrd, x + barWidth / 2, y - 18, sfOrd);
                }

                // X Axis Label
                var sfXLabel = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                g.DrawString(item.PeriodLabel, fontX, brushX, x + barWidth / 2, Height - bottomMargin + 8, sfXLabel);
            }
        }
    }
}
