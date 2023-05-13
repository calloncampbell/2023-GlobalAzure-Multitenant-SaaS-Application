using System.IO;
using System.Net;
using System.Threading.Tasks;
using ApiFunctionApp.Entities.AzureDB.Models;
using ApiFunctionApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace ApiFunctionApp.Functions.HttpTrigger
{
    public class NewOrder
    {
        private readonly ILogger<NewOrder> _logger;
        private readonly DataService _dataService;

        public NewOrder(
            ILogger<NewOrder> log, 
            DataService dataService)
        {
            _logger = log;
            _dataService = dataService;
        }

        [FunctionName("NewOrder")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "order" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a new order request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // create random customer between 0 and 5, product between 0 and 1
            var random = new System.Random();
            int customerId = (random.Next(0, 5)) + 100;
            int productId = random.Next(0, 1);
                        
            var order = new Order
            {                
                CustomerId = customerId,
                OrderDate = System.DateTime.Now,
                ProductId = productId
            };

            var result = await _dataService.AddOrderAsync(order);
            
            return new OkObjectResult(result);
        }
    }
}

