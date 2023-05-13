using ApiFunctionApp.Entities.AzureDB;
using ApiFunctionApp.Entities.AzureDB.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFunctionApp.Services
{
    public class DataService
    {
        private readonly ILogger<DataService> _logger;
        private AzureClient _azureClient;

        public DataService(
            ILogger<DataService> log,
            AzureClient azureClient)
        {
            _logger = log;
            _azureClient = azureClient;
        }

        public async Task<Customer> AddCustomerAsync(Customer customerData)
        {
            return await _azureClient.AddCustomerAsync(customerData);
        }

        public async Task<Customer> GetCustomerAsync(int customerId)
        {
            return await _azureClient.GetCustomerAsync(customerId);
        }               
        
        public async Task<Order> AddOrderAsync(Order orderData)
        {
            var customer = await _azureClient.GetCustomerAsync(orderData.CustomerId);
            if (customer == null)
            {
                await _azureClient.AddCustomerAsync(new Customer { CustomerId = orderData.CustomerId, Name = "Test Customer", RegionId = 1 });
            }
            var order = await _azureClient.AddOrderAsync(orderData);
            return order;
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync(int customerId)
        {
            return await _azureClient.GetOrdersAsync(customerId);
        }
    }
}
