using ApiFunctionApp.Entities.AzureDB;
using ApiFunctionApp.Entities.Shards;
using ApiFunctionApp.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(ApiFunctionApp.Startup))]
namespace ApiFunctionApp
{
    public class Startup : FunctionsStartup
    {
        private static IConfigurationRoot Configuration { get; set; }
        public IConfigurationBuilder ConfigurationBuilder { get; set; }

        public Startup()
        {
            ConfigurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddEnvironmentVariables();

            Configuration = ConfigurationBuilder.Build();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            Configuration = ConfigurationBuilder.Build();

            builder.Services.AddLogging();
            builder.Services.AddSingleton(Configuration);
            builder.Services.AddSingleton<Shard>();
            builder.Services.AddScoped<AzureClient>();
            builder.Services.AddScoped<DataService>();
        }
    }
}
