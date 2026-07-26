using Azure;
using Azure.Data.Tables;
using ABCRetail.Helpers;

namespace ABCRetail.Entities
{
    public class ProductEntity : ITableEntity
    {
        // Azure Table Storage Properties
        public string PartitionKey { get; set; } = StorageConstants.ProductsPartition;

        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }


        // Product Properties

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public double Price { get; set; }
        public int Stock { get; set; }

        public string Category { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
    }
}