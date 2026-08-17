namespace TommyPOS.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public decimal MinOrderAmount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
