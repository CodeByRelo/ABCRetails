using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Entities
{
    public class CustomerEntity : ITableEntity
    {
        // Azure Table Storage Properties
        public string PartitionKey { get; set; } = "Customers";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Customer Properties
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}