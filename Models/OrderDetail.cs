namespace TommyPOS.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string Size { get; set; } = "M"; // S, M, L
        public decimal SizePriceExtra { get; set; } = 0;
        public string Sugar { get; set; } = "100%"; // 0%, 30%, 50%, 70%, 100%
        public string Ice { get; set; } = "Bình thường"; // Không đá, Ít đá, Bình thường
        public string Toppings { get; set; } = string.Empty; // e.g., "Trân châu (+5k), Thạch (+5k)"
        public decimal ToppingPriceExtra { get; set; } = 0;
        public string Note { get; set; } = string.Empty;

        public decimal SingleItemTotal => UnitPrice + SizePriceExtra + ToppingPriceExtra;
        public decimal SubTotal => SingleItemTotal * Quantity;
    }
}
