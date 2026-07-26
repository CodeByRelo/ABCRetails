using ABCRetail.Models;

namespace ABCRetail.Interfaces
{
    public interface ICustomerTableService
    {
        Task AddCustomerAsync(Customer customer);

        Task<List<Customer>> GetCustomersAsync();

        Task<Customer?> GetCustomerAsync(string id);

        Task DeleteCustomerAsync(string id);
    }
}