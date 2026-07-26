namespace ABCRetail.Models
{
    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending";
    }
}