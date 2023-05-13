using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace ApiFunctionApp.Functions.HttpTrigger
{
    public class Welcome
    {
        private readonly ILogger<Welcome> _logger;
        private static IConfigurationRoot _configuration { set; get; }

        public Welcome(
            ILogger<Welcome> log,
            IConfigurationRoot configuration)
        {
            _logger = log;
            _configuration = configuration;
        }

        [FunctionName(nameof(Welcome))]
        [OpenApiOperation(operationId: "RunAsync", tags: new[] { "welcome" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            _logger.LogInformation($"HTTP trigger function processed a request for Welcome");

            var appName = Assembly.GetExecutingAssembly().GetName().Name;
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var functionRuntimeVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var regionName = Environment.GetEnvironmentVariable("REGION_NAME");

            var appMessage = $"Welcome to {appName}!";
            var responseMessage = new
            {
                Message = appMessage.Trim(),
                ApplicationName = appName,
                ApplicationVersion = appVersion,
                Region = regionName,
                FunctionRuntimeVersion = functionRuntimeVersion,
                CurrentDatetime = DateTimeOffset.Now
            };

            return new OkObjectResult(responseMessage);
        }
    }
}

