using System.IO;
using System.Net;
using System.Threading.Tasks;
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
    public class GetOrders
    {
        private readonly ILogger<GetOrders> _logger;
        private readonly DataService _dataService;

        public GetOrders(
            ILogger<GetOrders> log,
            DataService dataService)
        {
            _logger = log;
            _dataService = dataService;
        }

        [FunctionName("GetOrders")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "order" })]
        [OpenApiParameter(name: "customerId", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **CustomerId** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a get orders request.");
            
            int customerId = int.Parse(req.Query["customerId"]);

            var result = await _dataService.GetOrdersAsync(customerId);

            return new OkObjectResult(result);
        }
    }
}

