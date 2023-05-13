using ApiFunctionApp.Entities.AzureDB.Models;
using ApiFunctionApp.Entities.Extensions;
using ApiFunctionApp.Entities.Shards;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ApiFunctionApp.Entities.AzureDB
{
    public class AzureClient
    {
        private readonly int _commandTimeOut;
        private readonly IConfiguration _configuration;
        private readonly Shard _shard;

        public AzureClient(IConfiguration configuration, Shard shard)
        {
            _configuration = configuration;
            _commandTimeOut = int.Parse(_configuration[$"CommandTimeout"] ?? "300");
            _shard = shard;
        }

        public async Task<Customer> AddCustomerAsync(Customer customerData)
        {
            await _shard.ExecuteSqlAsync(customerData.CustomerId, AzureSQL.AddCustomer, new { customerData.CustomerId, customerData.Name, customerData.RegionId }, commandTimeout: _commandTimeOut);
            return customerData;
        }

        public async Task<Customer> GetCustomerAsync(int customerId)
        {
            var result = await _shard.QuerySqlAsync<Customer>(customerId, AzureSQL.GetCustomer, new { customerId }, commandTimeout: _commandTimeOut);
            return result.FirstOrDefault();
        }

        public async Task<Order> AddOrderAsync(Order orderData)
        {
            await _shard.ExecuteSqlAsync(orderData.CustomerId, AzureSQL.AddOrder, new { orderData.CustomerId, orderData.OrderDate, orderData.ProductId }, commandTimeout: _commandTimeOut);
            return orderData;
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync(int customerId)
        {
            var orders = await _shard.QuerySqlAsync<Order>(customerId, AzureSQL.GetOrders, new { customerId }, commandTimeout: _commandTimeOut);
            return orders;
        }        
    }
}
