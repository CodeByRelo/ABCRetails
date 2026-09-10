using ABCRetail.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class FunctionService
    {
        private readonly HttpClient _httpClient;

        public FunctionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> StoreCustomerAsync(Customer customer)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "StoreTableFunction",
                    new
                    {
                        type = "customer",
                        customer.Id,
                        customer.FirstName,
                        customer.LastName,
                        customer.Email,
                        customer.PhoneNumber
                    });

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> StoreProductAsync(Product product)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "StoreTableFunction",
                    new
                    {
                        type = "product",
                        product.Id,
                        product.Name,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.Category,
                        product.ImageUrl
                    });

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> UploadProductImageAsync(
            IFormFile imageFile)
        {
            using var memoryStream = new MemoryStream();

            await imageFile.CopyToAsync(memoryStream);

            var request = new
            {
                fileName = imageFile.FileName,
                contentBase64 =
                    Convert.ToBase64String(memoryStream.ToArray())
            };

            var response =
                await _httpClient.PostAsJsonAsync(
                    "WriteBlobFunction",
                    request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SendOrderAsync(Order order)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "QueueFunction",
                    order);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> WriteLogAsync(string message)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "FileFunction",
                    new
                    {
                        message
                    });

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}