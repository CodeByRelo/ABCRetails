using ABCRetail.Models;

namespace ABCRetail.Interfaces
{
    public interface IProductTableService
    {
        Task AddProductAsync(Product product);

        Task<List<Product>> GetProductsAsync();

        Task<Product?> GetProductAsync(string id);

        Task DeleteProductAsync(string id);
    }
}