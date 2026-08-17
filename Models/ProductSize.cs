namespace TommyPOS.Models
{
    public class ProductSize
    {
        public int Id { get; set; }
        public int ProductId { get; set; }       // 0 = áp dụng cho tất cả món (default)
        public string SizeLabel { get; set; } = "M";  // S, M, L, XL, ...
        public decimal PriceExtra { get; set; } = 0;
        public int DisplayOrder { get; set; } = 0;

        public override string ToString() => $"{SizeLabel} (+{PriceExtra:N0}đ)";
    }
}
