namespace TommyPOS.Models
{
    public enum TableStatus
    {
        Available,
        Occupied,
        Reserved
    }

    public class DiningTable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; } = 4;
        public TableStatus Status { get; set; } = TableStatus.Available;
        public int? CurrentOrderId { get; set; }
        public decimal CurrentTotal { get; set; } = 0;
    }
}
