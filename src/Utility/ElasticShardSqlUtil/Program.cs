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
                                services.AddSingleton<IRecoveryManagement, RecoveryManagement>();
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
            var root = new RootCommand("Elastic Scale SQL Utility for database shard management.");

            // Command: shard-map-manager
            var commandShardMapManager = new Command("shard-map-manager", "Manage the Shard Map Manager.");

            Command commandShardMapManagerCreate = new Command("create", "Creates the Shard Map Manager.");
            commandShardMapManagerCreate.Handler = CommandHandler.Create<ShardMapManagerOptions, IHost>(ShardMapManagerCommandCreate);
            commandShardMapManager.AddCommand(commandShardMapManagerCreate);

            Command commandShardMapManagerStatus = new Command("status", "Gets the status of the Shard Map Manager.");
            commandShardMapManagerStatus.Handler = CommandHandler.Create<ShardMapManagerOptions, IHost>(ShardMapManagerCommandStatus);
            commandShardMapManager.AddCommand(commandShardMapManagerStatus);

            Command commandShardMapManagerCleanup = new Command("cleanup", "Clean up empty shards in the Shard Map Manager.");
            commandShardMapManagerCleanup.Handler = CommandHandler.Create<ShardMapManagerOptions, IHost>(ShardMapManagerCommandCleanup);
            commandShardMapManager.AddCommand(commandShardMapManagerCleanup);

            root.AddCommand(commandShardMapManager);

            // Command: shard
            var commandShard = new Command("shard", "Manage the shards in Shard Map Manager.");

            Command commandShardAdd = new Command("add", "Adds a new shard to the Shard Map Manager. \n\nExamples:\n  .\\ElasticShardSqlUtil.exe shard add --tenants 2871 --tenant-type database-per-tenant \n  .\\ElasticShardSqlUtil.exe shard add --tenants 1000 2000 --tenant-type sharded-multi-tenant --database-name demos")
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
                new Option<string>(new []{"--file", "-f"}, "The SQL script to execute for the new shard.")
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


            // Command: recovery-manager
            var commandRecoveryManager = new Command("recovery-manager", "Manage the Recovery Manager.");

            Command commandRecoveryManagerDetachShard = new Command("detach-shard", "Detaches the given shard from the shard map and deletes mappings associated with the shard."){
                new Option<int[]>(new []{ "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                }
            };
            commandRecoveryManagerDetachShard.Handler = CommandHandler.Create<RecoveryOptions, IHost>(RecoveryManagerCommandDetachShard);
            commandRecoveryManager.AddCommand(commandRecoveryManagerDetachShard);

            Command commandRecoveryManagerDetechMappingIssues = new Command("detect-mapping-issues", "Gets a shard maps (either local or global) as the source of truth and reconciles mappings on both shard maps (GSM and LSM).") {
                new Option<int[]>(new[] { "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                }
            };
            commandRecoveryManagerDetechMappingIssues.Handler = CommandHandler.Create<RecoveryOptions, IHost>(RecoveryManagerCommandDetectMappingIssues);
            commandRecoveryManager.AddCommand(commandRecoveryManagerDetechMappingIssues);

            Command commandRecoveryManagerResolveMappingIssues = new Command("resolve-mapping-issues", "") {
                new Option<int[]>(new []{ "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                },
                new Option<string>(new []{"--resolution-type", "-y"}, "The mapping difference resolution type. Supported options are 'KeepShardMapMapping' for global shard map (GSM) and 'KeepShardMapping' for local shard map (LSM). It's recommended to use 'KeepShardMapping'.")
                {
                    IsRequired = true,
                }
            };
            commandRecoveryManagerResolveMappingIssues.Handler = CommandHandler.Create<RecoveryOptions, IHost>(RecoveryManagerCommandResolveMappingIssues);
            commandRecoveryManager.AddCommand(commandRecoveryManagerResolveMappingIssues);

            Command commandRecoveryManagerAttachShard = new Command("attach-shard", "") {
                new Option<int[]>(new []{ "--tenants", "-t" }, "Space separated list of tenant IDs.")
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = true
                }
            };
            commandRecoveryManagerAttachShard.Handler = CommandHandler.Create<RecoveryOptions, IHost>(RecoveryManagerCommandAttachShard);
            commandRecoveryManager.AddCommand(commandRecoveryManagerAttachShard);

            root.AddCommand(commandRecoveryManager);

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
        /// Cleanup the Shard Map Manager database.
        /// </summary>
        /// <param name="host"></param>
        private static void ShardMapManagerCommandCleanup(ShardMapManagerOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IShardManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = ShardMapManagerActions.Cleanup;

            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Shard Map Manager action was requested for 'cleanup'");
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void RecoveryManagerCommandDetachShard(RecoveryOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IRecoveryManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = RecoveryOptionActions.Detach;

            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Recovery Manager action was requested for 'detach-shard' for: {Tenants}", options.Tenants);
            service.RecoveryManagementAction(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void RecoveryManagerCommandDetectMappingIssues(RecoveryOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IRecoveryManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = RecoveryOptionActions.Detect;

            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Recovery Manager action was requested for 'detect-mapping-issues' for: {Tenants}", options.Tenants);
            service.RecoveryManagementAction(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void RecoveryManagerCommandResolveMappingIssues(RecoveryOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IRecoveryManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = RecoveryOptionActions.Resolve;

            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Recovery Manager action was requested for 'resolve-mapping-issues' for: {Tenants}", options.Tenants);
            service.RecoveryManagementAction(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="host"></param>
        private static void RecoveryManagerCommandAttachShard(RecoveryOptions options, IHost host)
        {
            var serviceProvider = host.Services;
            var service = serviceProvider.GetRequiredService<IRecoveryManagement>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            options.Action = RecoveryOptionActions.Attach;

            logger.LogInformation(LogEvents.ShardMapManagerEvent, "Recovery Manager action was requested for 'attach-shard' for: {Tenants}", options.Tenants);
            service.RecoveryManagementAction(options);
        }
    }
}