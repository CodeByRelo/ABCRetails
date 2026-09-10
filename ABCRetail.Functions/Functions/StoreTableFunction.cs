using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace ABCRetail.Functions.Functions
{
    public class StoreTableFunction
    {
        private readonly TableClient _tableClient;

        public StoreTableFunction()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("StorageConnection");

            var tableName =
                Environment.GetEnvironmentVariable("TableName");

            _tableClient = new TableClient(
                connectionString,
                tableName);

            _tableClient.CreateIfNotExists();
        }

        [Function("StoreTableFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData req)
        {
            var requestBody =
                await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Request body cannot be empty.");

                return badResponse;
            }

            using JsonDocument document =
                JsonDocument.Parse(requestBody);

            if (!document.RootElement.TryGetProperty(
                    "type",
                    out JsonElement typeElement))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Type is required. Use 'customer' or 'product'.");

                return badResponse;
            }

            string type =
                typeElement.GetString()?.ToLower() ?? "";

            if (type == "customer")
            {
                var customer =
                    JsonSerializer.Deserialize<CustomerRequest>(
                        requestBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (customer == null ||
                    string.IsNullOrWhiteSpace(customer.FirstName) ||
                    string.IsNullOrWhiteSpace(customer.LastName) ||
                    string.IsNullOrWhiteSpace(customer.Email))
                {
                    var badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "FirstName, LastName and Email are required.");

                    return badResponse;
                }

                var customerEntity =
                    new TableEntity(
                        "Customers",
                        string.IsNullOrWhiteSpace(customer.Id)
                            ? Guid.NewGuid().ToString()
                            : customer.Id)
                    {
                        ["FirstName"] = customer.FirstName,
                        ["LastName"] = customer.LastName,
                        ["Email"] = customer.Email,
                        ["PhoneNumber"] = customer.PhoneNumber ?? ""
                    };

                await _tableClient.AddEntityAsync(customerEntity);

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    message = "Customer stored successfully.",
                    partitionKey = "Customers",
                    rowKey = customerEntity.RowKey
                });

                return response;
            }

            if (type == "product")
            {
                var product =
                    JsonSerializer.Deserialize<ProductRequest>(
                        requestBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (product == null ||
                    string.IsNullOrWhiteSpace(product.Name) ||
                    string.IsNullOrWhiteSpace(product.Description))
                {
                    var badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Name and Description are required.");

                    return badResponse;
                }

                var productEntity =
                    new TableEntity(
                        "Products",
                        string.IsNullOrWhiteSpace(product.Id)
                            ? Guid.NewGuid().ToString()
                            : product.Id)
                    {
                        ["Name"] = product.Name,
                        ["Description"] = product.Description,
                        ["Price"] = (double)product.Price,
                        ["Stock"] = product.Stock,
                        ["Category"] = product.Category ?? "",
                        ["ImageUrl"] = product.ImageUrl ?? ""
                    };

                await _tableClient.AddEntityAsync(productEntity);

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    message = "Product stored successfully.",
                    partitionKey = "Products",
                    rowKey = productEntity.RowKey
                });

                return response;
            }

            var invalidResponse =
                req.CreateResponse(HttpStatusCode.BadRequest);

            await invalidResponse.WriteStringAsync(
                "Invalid type. Use 'customer' or 'product'.");

            return invalidResponse;
        }
    }

    public class CustomerRequest
    {
        public string Id { get; set; } = "";

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string? PhoneNumber { get; set; }
    }

    public class ProductRequest
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string Category { get; set; } = "";

        public string? ImageUrl { get; set; }
    }
}