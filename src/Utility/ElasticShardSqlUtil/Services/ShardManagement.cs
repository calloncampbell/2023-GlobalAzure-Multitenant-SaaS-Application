using ElasticShardSqlUtil.Interfaces;
using ElasticShardSqlUtil.Models;
using ElasticShardSqlUtil.Options;
using ElasticShardSqlUtil.Utils;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement.Schema;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticShardSqlUtil.Services
{
    internal class ShardManagement : IShardManagement
    {
        /// <summary>
        /// The shard map manager, or null if it does not exist. 
        /// It is recommended that you keep only one shard map manager instance in
        /// memory per AppDomain so that the mapping cache is not duplicated.
        /// </summary>
        static ShardMapManager s_shardMapManager;

        /// <summary>
        /// The shard map, or null if it does not exist.
        /// </summary>
        public ShardManagement()
        {
            if (!SqlDatabaseUtils.TryConnectToSqlDatabase())
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("Press ENTER to continue...");
                    Console.ReadLine();
                }
                return;
            }
            
            s_shardMapManager = ShardManagementUtils.TryGetShardMapManager(
               ConfigurationUtils.ShardMapManagerServerName,
               ConfigurationUtils.ShardMapManagerDatabaseName);
        }

        /// <summary>
        /// Creates a new shard map manager database and shard map.
        /// </summary>
        /// <param name="options"></param>
        public void ShardMapManagerAction(ShardMapManagerOptions options)
        {
            if (options.Action == ShardMapManagerActions.Create)
            {
                CreateShardMapManager();
            }
            else if (options.Action == ShardMapManagerActions.Status)
            {
                GetShardMapStatus();
            }
            else
            {
                ConsoleUtils.WriteWarning($"Shard Map Manager action is '{options.Action}' not supported.");
            }
        }

        /// <summary>
        /// Creates a new shard map manager database.
        /// </summary>
        private void CreateShardMapManager()
        {
            if (s_shardMapManager != null)
            {
                ConsoleUtils.WriteWarning("Shard Map Manager already exists");
                return;
            }

            // Check catalog database exists
            if (!SqlDatabaseUtils.DatabaseExists(ConfigurationUtils.ShardMapManagerServerName, ConfigurationUtils.ShardMapManagerDatabaseName))
            {
                ConsoleUtils.WriteError($"Failed to initialize shard map manager from '{ConfigurationUtils.ShardMapManagerDatabaseName}' database. Check the configuration file for the correct database name and ensure the catalog is initialized. If the database needs to be created, use the bicep script.");
                return;
            }

            // Initialize shard map manager from catalog database
            string shardMapManagerConnectionString = ConfigurationUtils.GetConnectionString(ConfigurationUtils.ShardMapManagerServerName, ConfigurationUtils.ShardMapManagerDatabaseName);
            s_shardMapManager = ShardManagementUtils.CreateOrGetShardMapManager(shardMapManagerConnectionString);

            if (s_shardMapManager == null)
            {
                ConsoleUtils.WriteError($"Failed to load shard map '{ConfigurationUtils.ShardMapName}' from '{ConfigurationUtils.ShardMapManagerDatabaseName}' database. Ensure catalog is initialized.");
                return;
            }

            // Initialize shard map
            ListShardMap<int> listShardMap = ShardManagementUtils.CreateOrGetListShardMap<int>(s_shardMapManager, ConfigurationUtils.ShardMapName);

            // Create schema info so that the split-merge service can be used to move data in sharded tables and reference tables.
            CreateSchemaInfo(listShardMap.Name);
        }

        /// <summary>
        /// Creates schema info for the shard map.
        /// </summary>
        private static void GetShardMapStatus()
        {
            ConsoleUtils.WriteMessage("Current Shard Map state:");

            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap == null)
            {
                // TODO
                return;
            }

            // Get all shards
            IEnumerable<Shard> allShards = shardMap.GetShards();

            // Get all mappings, grouped by the shard that they are on. We do this all in one go to minimise round trips.
            ILookup<Shard, PointMapping<int>> mappingsGroupedByShard = shardMap.GetMappings().ToLookup(m => m.Shard);

            if (allShards.Any())
            {
                // The shard map contains some shards, so for each shard (sorted by database name)
                // write out the mappings for that shard
                foreach (Shard shard in shardMap.GetShards().OrderBy(s => s.Location.Database))
                {
                    IEnumerable<PointMapping<int>> mappingsOnThisShard = mappingsGroupedByShard[shard];

                    if (mappingsOnThisShard.Any())
                    {
                        string mappingsString = string.Join(", ", mappingsOnThisShard.Select(m => m.Value));
                        Console.WriteLine("\t{0} contains key list {1}", shard.Location.Database, mappingsString);
                    }
                    else
                    {
                        Console.WriteLine("\t{0} contains no key listings.", shard.Location.Database);
                    }
                }
            }
            else
            {
                Console.WriteLine("\tShard Map contains no shards");
            }
        }

        /// <summary>
        /// Creates schema info for the shard map manager.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public void AddShardAction(ShardOptions options)
        {
            if (options.Tenants.Length == 0)
            {
                throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            }

            if (string.IsNullOrEmpty(options.TenantType))
            {
                throw new Exception("Missing command line argument for tenant-type. Try again with one of the supported types.");
            }

            if (options.TenantType.ToLower() == "database-per-tenant")
            {
                AddDatabasePerTenantShard(options);
            }
            else if (options.TenantType.ToLower() == "sharded-multi-tenant")
            {
                AddShardedMultitenantDatabaseShard(options);
            }
            else
            {
                throw new Exception("Tenant type not supported. Try again with one of the supported types.");
            }

                     
        }

        /// <summary>
        /// Creates a new shard for a database-per-tenant scenario.
        /// </summary>
        /// <param name="options"></param>
        private void AddDatabasePerTenantShard(ShardOptions options)
        {
            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap == null)
            {
                return;
            }

            foreach (var id in options.Tenants)
            {
                ConsoleUtils.WriteInfo($"\nProcessing tenant '{id}'...");

                // Choose the shard name
                string databaseName = string.Format(ConfigurationUtils.ShardDatabaseNameFormat, id);

                // Check that the database exists
                if (!SqlDatabaseUtils.DatabaseExists(ConfigurationUtils.ShardMapManagerServerName, databaseName))
                {
                    ConsoleUtils.WriteError($"Failed to initialize shard map manager from '{ConfigurationUtils.ShardMapManagerDatabaseName}' database. Check the configuration file for the correct database name and ensure the catalog is initialized. If the database needs to be created, use the bicep script.");
                    continue;
                }

                // Add it to the shard map
                ShardLocation shardLocation = new ShardLocation(ConfigurationUtils.ShardMapManagerServerName, databaseName);
                var shard = ShardManagementUtils.CreateShard(shardMap, shardLocation);
                if (shard != null)
                {
                    // Create a mapping to that shard.
                    PointMapping<int> mappingForNewShard = shardMap.CreatePointMapping(id, shard);

                    ConsoleUtils.WriteSuccess($"Point mapping created for tenant '{id}' in shard '{shard.Location.Database}'");
                }
            }
        }

        /// <summary>
        /// Creates a new shard for a sharded-multi-tenant scenario.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        private void AddShardedMultitenantDatabaseShard(ShardOptions options)
        {
            if (string.IsNullOrEmpty(options.DatabaseName))
            {
                throw new Exception("Missing command line argument for database-name. Try again with format 'ShardedMultiTenantDatabase'");
            }

            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap == null)
            {
                return;
            }

            // Choose the shard name
            string databaseName = string.Format(ConfigurationUtils.ShardDatabaseNameFormat, options.DatabaseName);

            // Check that the database exists
            if (!SqlDatabaseUtils.DatabaseExists(ConfigurationUtils.ShardMapManagerServerName, databaseName))
            {
                ConsoleUtils.WriteError($"Failed to initialize shard map manager from '{ConfigurationUtils.ShardMapManagerDatabaseName}' database. Check the configuration file for the correct database name and ensure the catalog is initialized. If the database needs to be created, use the bicep script.");
            }

            // Add new shard location to list shard map
            ShardLocation shardLocation = new ShardLocation(ConfigurationUtils.ShardMapManagerServerName, databaseName);
            var shard = ShardManagementUtils.GetShard(shardMap, shardLocation);
            if (shard == null)
            {
                shard = ShardManagementUtils.CreateShard(shardMap, shardLocation);
            }

            foreach (var id in options.Tenants)
            {
                ConsoleUtils.WriteInfo($"\nProcessing tenant '{id}'...");

                var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
                if (pointMapping != null && pointMapping.Value == id)
                {
                    ConsoleUtils.WriteWarning($"Tenant '{id}' already exists in shard '{shard.Location.Database}'");
                    continue;
                }

                // Create a mapping to that shard.
                PointMapping<int> mappingForNewShard = shardMap.CreatePointMapping(id, shard);

                ConsoleUtils.WriteSuccess($"Point mapping created for tenant '{id}' in shard '{shard.Location.Database}'");
            }            
        }

        /// <summary>
        /// Creates schema info for the shard map manager.
        /// </summary>
        /// <param name="options"></param>
        public void GetShardAction(ShardOptions options)
        {
            if (options.Tenants.Length == 0)
            {
                throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            }

            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap == null)
            {
                return;
            }

            ConsoleUtils.WriteInfo($"\nProcessing tenants...");
            foreach (var id in options.Tenants)
            {                
                var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
                if (pointMapping != null && pointMapping.Value == id)                    
                {                    
                    Console.WriteLine("\tTenant '{0}' status is '{1}' and is mapped to server '{2}', database '{3}'", pointMapping.Value, pointMapping.Status.ToString(), pointMapping.Shard.Location.Server, pointMapping.Shard.Location.Database);                    
                }
                else
                {
                    ConsoleUtils.WriteWarning($"Tenant '{id}' doesn't exist in the shard map manager.");                 
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public void DeleteShardAction(ShardOptions options)
        {            
            if (options.Tenants.Length == 0)
            {
                throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            }

            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap == null)
            {
                return;
            }

            foreach (var id in options.Tenants)
            {
                ConsoleUtils.WriteInfo($"\nProcessing tenant '{id}'...");

                var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
                if (pointMapping != null && pointMapping.Value == id)
                {
                    PointMappingUpdate pointMappingUpdate = new PointMappingUpdate();
                    pointMappingUpdate.Status = MappingStatus.Offline;
                    pointMapping = shardMap.UpdateMapping(pointMapping, pointMappingUpdate);
                    Console.WriteLine("\tTenant '{0}' status has been set to 'offline'...", pointMapping.Value);

                    var serverName = pointMapping.Shard.Location.Server;
                    var databaseName = pointMapping.Shard.Location.Database;

                    shardMap.DeleteMapping(pointMapping);
                    Console.WriteLine("\tTenant '{0}' has been deleted from server '{1}', database '{2}'", id, serverName, databaseName);
                }
                else
                {
                    ConsoleUtils.WriteWarning($"Tenant '{id}' doesn't exist in the shard map manager.");
                }
            }
        }

        /// <summary>
        /// Run SQL script across all shards for list of tenants provided
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public void SqlScriptShardAction(ShardOptions options)
        {
            if (string.IsNullOrEmpty(options.File))
            {
                throw new Exception("Missing command line argument for file.");
            }

            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap == null)
            {
                // TODO
                return;
            }

            // Get all shards
            IEnumerable<Shard> allShards = shardMap.GetShards();

            // Get all mappings, grouped by the shard that they are on. We do this all in one go to minimise round trips.
            ILookup<Shard, PointMapping<int>> mappingsGroupedByShard = shardMap.GetMappings().ToLookup(m => m.Shard);

            if (allShards.Any())
            {
                // The shard map contains some shards, so for each shard (sorted by database name)
                // write out the mappings for that shard
                foreach (Shard shard in shardMap.GetShards().OrderBy(s => s.Location.Database))
                {
                    IEnumerable<PointMapping<int>> mappingsOnThisShard = mappingsGroupedByShard[shard];
                    
                    if (mappingsOnThisShard.Any())
                    {
                        string mappingsString = string.Join(", ", mappingsOnThisShard.Select(m => m.Value));
                        
                        ConsoleUtils.WriteMessage("Processing SQL script against database {0}...", shard.Location.Database);
                        ShardManagementUtils.ApplySqlSchemaToShard(shard.Location.Database, options.File);
                    }
                    else
                    {
                        ConsoleUtils.WriteWarning("Database {0} contains no key listings. SQL Script will not be applied.", shard.Location.Database);
                    }
                }
            }
            else
            {
                ConsoleUtils.WriteWarning("Shard Map contains no shards");
            }
        }

        /// <summary>
        /// Creates the schema info so that the split-merge service can be used to move data in sharded tables and reference tables.
        /// </summary>
        /// <param name="shardMapName"></param>
        private static void CreateSchemaInfo(string shardMapName)
        {
            // Create schema info
            SchemaInfo schemaInfo = new SchemaInfo();
            
            // Add schema info for references tables
            foreach(var item in ConfigurationUtils.GetDatabaseReferenceTables())
            {
                schemaInfo.Add(new ReferenceTableInfo(item));
            }

            // Add schema info for sharded tables            
            foreach (var item in ConfigurationUtils.GetDatabaseShardTables())
            {
                schemaInfo.Add(new ShardedTableInfo(item.TableName, item.KeyColumnName));
            }

            // Register it with the shard map manager for the given shard map name
            s_shardMapManager.GetSchemaInfoCollection().Add(shardMapName, schemaInfo);
        }
    }
}
