using System;
using System.Collections.Generic;

namespace TommyPOS.Models
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int? TableId { get; set; }
        public string TableName { get; set; } = "Mang về";
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CashGiven { get; set; }
        public decimal ChangeAmount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string PaymentMethod { get; set; } = "Tiền mặt"; // Tiền mặt, Chuyển khoản, Ví điện tử
        public string CashierName { get; set; } = "Thu Ngân";
        public List<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    }
}
