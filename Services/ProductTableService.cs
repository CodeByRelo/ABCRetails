using Azure;
using Azure.Data.Tables;
using ABCRetail.Configuration;
using ABCRetail.Entities;
using ABCRetail.Helpers;
using ABCRetail.Interfaces;
using ABCRetail.Models;
using Microsoft.Extensions.Options;

namespace ABCRetail.Services
{
    public class ProductTableService : IProductTableService
    {
        private readonly TableClient _tableClient;
        private readonly IBlobStorageService _blobService;

        public ProductTableService(
            IOptions<StorageSettings> options,
            IBlobStorageService blobService)
        {
            var settings = options.Value;

            _tableClient = new TableClient(
                settings.ConnectionString,
                settings.TableName);

            _tableClient.CreateIfNotExists();

            _blobService = blobService;
        }

        // =========================
        // Add Product
        // =========================

        public async Task AddProductAsync(Product product)
        {
            var entity = new ProductEntity
            {
                PartitionKey = StorageConstants.ProductsPartition,
                RowKey = product.Id,

                Name = product.Name,
                Description = product.Description,
                Price = (double)product.Price,
                Stock = product.Stock,
                Category = product.Category,

                // Store only the blob filename
                ImageUrl = product.ImageUrl
            };

            await _tableClient.AddEntityAsync(entity);
        }

        // =========================
        // Get All Products
        // =========================

        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();

            await foreach (var entity in _tableClient.QueryAsync<ProductEntity>(
                x => x.PartitionKey == StorageConstants.ProductsPartition))
            {
                products.Add(new Product
                {
                    Id = entity.RowKey,
                    Name = entity.Name,
                    Description = entity.Description,
                    Price = (decimal)entity.Price,
                    Stock = entity.Stock,
                    Category = entity.Category,

                    // Generate a fresh SAS URL every time
                    ImageUrl = _blobService.GetImageUrl(entity.ImageUrl)
                });
            }

            return products;
        }

        // =========================
        // Get Single Product
        // =========================

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ProductEntity>(
                    StorageConstants.ProductsPartition,
                    id);

                var entity = response.Value;

                return new Product
                {
                    Id = entity.RowKey,
                    Name = entity.Name,
                    Description = entity.Description,
                    Price = (decimal)entity.Price,
                    Stock = entity.Stock,
                    Category = entity.Category,

                    // Generate a fresh SAS URL every time
                    ImageUrl = _blobService.GetImageUrl(entity.ImageUrl)
                };
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        // =========================
        // Delete Product
        // =========================

        public async Task DeleteProductAsync(string id)
        {
            await _tableClient.DeleteEntityAsync(
                StorageConstants.ProductsPartition,
                id);
        }
    }
}