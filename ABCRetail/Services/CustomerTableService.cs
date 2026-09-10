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
    public class CustomerTableService : ICustomerTableService
    {
        private readonly TableClient _tableClient;

        public CustomerTableService(IOptions<StorageSettings> options)
        {
            var settings = options.Value;

            _tableClient = new TableClient(
                settings.ConnectionString,
                settings.TableName);

            _tableClient.CreateIfNotExists();
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            var entity = new CustomerEntity
            {
                PartitionKey = StorageConstants.CustomersPartition,
                RowKey = customer.Id,

                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber
            };

            await _tableClient.AddEntityAsync(entity);
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var customers = new List<Customer>();

            await foreach (var entity in _tableClient.QueryAsync<CustomerEntity>(
                x => x.PartitionKey == StorageConstants.CustomersPartition))
            {
                customers.Add(new Customer
                {
                    Id = entity.RowKey,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    Email = entity.Email,
                    PhoneNumber = entity.PhoneNumber
                });
            }

            return customers;
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<CustomerEntity>(
                    StorageConstants.CustomersPartition,
                    id);

                var entity = response.Value;

                return new Customer
                {
                    Id = entity.RowKey,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    Email = entity.Email,
                    PhoneNumber = entity.PhoneNumber
                };
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public async Task DeleteCustomerAsync(string id)
        {
            await _tableClient.DeleteEntityAsync(
                StorageConstants.CustomersPartition,
                id);
        }
    }
}