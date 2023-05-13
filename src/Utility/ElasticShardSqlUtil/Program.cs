using ElasticShardSqlUtil.Interfaces;
using ElasticShardSqlUtil.Models;
using ElasticShardSqlUtil.Options;
using ElasticShardSqlUtil.Services;
using ElasticShardSqlUtil.Utils;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace ElasticShardSqlUtil
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            try
            {
                ConfigurationUtils.InitializeConfiguration();                                

                await BuildCommandLine()
                    .UseHost(_ => Host.CreateDefaultBuilder(),
                        host =>
                        {
                            host.ConfigureServices(services =>
                            {
                                services.AddSingleton<IShardManagement, ShardManagement>();
                            });
                        })
                    .UseDefaults()
                    .Build()
                    .InvokeAsync(args);

            }
            catch (Exception ex)
            {
                ConsoleUtils.WriteError(ex.Message);
            }
        }

        /// <summary>
        /// Builds the command line.
        /// </summary>
        /// <returns></returns>
        public static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand("Elastic Shard SQL Utility for database shard management.");

            // Command: shard-map-manager
            var commandShardMapManager = new Command("shard-map-manager", "Manage the Shard Map Manager.");

            Command commandShardMapManagerCreate = new Command("create", "Creates the Shard Map Manager.");
            commandShardMapManagerCreate.Handler = CommandHandler.Create<ShardMapManagerOptions, IHost>(ShardMapManagerCommandCreate);
            commandShardMapManager.AddCommand(commandShardMapManagerCreate);

            Command commandShardMapManagerStatus = new Command("status", "Gets the status of the Shard Map Manager.");
            commandShardMapManagerStatus.Handler = CommandHandler.Create<ShardMapManagerOptions, IHost>(ShardMapManagerCommandStatus);
            commandShardMapManager.AddCommand(commandShardMapManagerStatus);
                        
            root.AddCommand(commandShardMapManager);

            // Command: shard
            var commandShard = new Command("shard", "Manage the shards in Shard Map Manager.");            
            
            Command commandShardAdd = new Command("add", "Adds a new shard to the Shard Map Manager. \n\nExamples:\n  .\\ElasticShardSqlUtil.exe shard add --tenants 1000 --tenant-type database-per-tenant \n  .\\ElasticShardSqlUtil.exe shard add --tenants 2000 2001 --tenant-type sharded-multi-tenant --database-name demos")
            {
                new Option<int[]>(new []{ "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                },
                new Option<string>(new []{"--tenant-type", "-y"}, "The type of tenant to setup. Supported options are 'database-per-tenant' and 'sharded-multi-tenant'.")
                {
                    IsRequired = true,                    
                },
                new Option<string>(new []{"--database-name", "-d"}, "The name of the database for the sharded multi-tenancy. This is required when the tenant type is 'sharded-multi-tenant'.")
                {
                    IsRequired = false,
                },
            };
            commandShardAdd.Handler = CommandHandler.Create<ShardOptions, IHost>(AddShardCommand);
            commandShard.AddCommand(commandShardAdd);

            Command commandShardGet = new Command("get", "Gets the shard details in the Shard Map Manager.")
            {
                new Option<int[]>(new []{ "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                }
            };
            commandShardGet.Handler = CommandHandler.Create<ShardOptions, IHost>(GetShardCommand);
            commandShard.AddCommand(commandShardGet);

            Command commandShardDelete = new Command("delete", "Deletes the shard details in the Shard Map Manager.")
            {
                new Option<int[]>(new []{ "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                }
            };
            commandShardDelete.Handler = CommandHandler.Create<ShardOptions, IHost>(DeleteShardCommand);
            commandShard.AddCommand(commandShardDelete);
            
            Command commandShardSqlScript = new Command("sql-script", "Runs SQL script for all shards in the Shard Map Manager. \n\nExamples:\n  .\\ElasticShardSqlUtil.exe shard sql-script --file script.sql")
            {                
                new Option<string>(new []{"--file", "-f"}, "The SQL script to execute.")
                {
                    IsRequired = true,
                },
            };
            commandShardSqlScript.Handler = CommandHandler.Create<ShardOptions, IHost>(SqlScriptShardCommand);
            commandShard.AddCommand(commandShardSqlScript);

            
            root.AddCommand(commandShard);
            
            return new CommandLineBuilder(root);
        }

        /// <summary>
        /// Creates the Shard Map Manager database.
        /// </summary>
        /// <param name="host"></param>
        private static void ShardMapManagerCommandCreate(ShardMapManagerOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = ShardMapManagerActions.Create;
            
            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Shard Map Manager action was requested for 'create'");
            service.ShardMapManagerAction(options);
        }

        /// <summary>
        /// Displays the status of the Shard Map Manager database.
        /// </summary>
        /// <param name="host"></param>
        private static void ShardMapManagerCommandStatus(ShardMapManagerOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = ShardMapManagerActions.Status;
            
            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Shard Map Manager action was requested for 'status'");
            service.ShardMapManagerAction(options);
        }

        /// <summary>
        /// Adds a new shard to the Shard Map Manager.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void AddShardCommand(ShardOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));

            logger.LogInformation(LogEvents.AddShardEvent, "Add Shard action was requested for: {Tenants}", options.Tenants);
            service.AddShardAction(options);
        }

        /// <summary>
        /// Gets the shard details in the Shard Map Manager.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void GetShardCommand(ShardOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));

            logger.LogInformation(LogEvents.AddShardEvent, "Get Shard action was requested for: {Tenants}", options.Tenants);
            service.GetShardAction(options);
        }

        /// <summary>
        /// Deletes the shard details in the Shard Map Manager.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void DeleteShardCommand(ShardOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));

            logger.LogInformation(LogEvents.AddShardEvent, "Delete Shard action was requested for: {Tenants}", options.Tenants);
            service.DeleteShardAction(options);
        }

        /// <summary>
        /// Runs SQL script for a shard in the Shard Map Manager.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void SqlScriptShardCommand(ShardOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));

            logger.LogInformation(LogEvents.AddShardEvent, "SQL Script Shard action was requested for: {Tenants}", options.Tenants);
            service.SqlScriptShardAction(options);
        }
    }
}