using System;
using System.Configuration;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

namespace ElasticShardSqlUtil.Utils
{
    internal static class ShardManagementUtils
    {
        /// <summary>
        /// Tries to get the ShardMapManager that is stored in the specified database.
        /// </summary>
        public static ShardMapManager TryGetShardMapManager(string shardMapManagerServerName, string shardMapManagerDatabaseName)
        {
            string shardMapManagerConnectionString =
                    ConfigurationUtils.GetConnectionString(
                        ConfigurationUtils.ShardMapManagerServerName,
                        ConfigurationUtils.ShardMapManagerDatabaseName);

            if (!SqlDatabaseUtils.DatabaseExists(shardMapManagerServerName, shardMapManagerDatabaseName))
            {
                // Shard Map Manager database has not yet been created
                return null;
            }

            ShardMapManager shardMapManager;
            bool smmExists = ShardMapManagerFactory.TryGetSqlShardMapManager(
                shardMapManagerConnectionString,
                ShardMapManagerLoadPolicy.Lazy,
                out shardMapManager);

            if (!smmExists)
            {
                // Shard Map Manager database exists, but Shard Map Manager has not been created
                return null;
            }

            return shardMapManager;
        }

        /// <summary>
        /// Creates a shard map manager in the database specified by the given connection string.
        /// </summary>
        public static ShardMapManager CreateOrGetShardMapManager(string shardMapManagerConnectionString)
        {
            ConsoleUtils.WriteMessage($"Checking if Shard Map Manager has been created...");

            // Get shard map manager database connection string
            // Try to get a reference to the Shard Map Manager in the Shard Map Manager database. If it doesn't already exist, then create it.
            ShardMapManager shardMapManager;
            bool shardMapManagerExists = ShardMapManagerFactory.TryGetSqlShardMapManager(
                shardMapManagerConnectionString,
                ShardMapManagerLoadPolicy.Lazy,
                out shardMapManager);

            if (shardMapManagerExists)
            {
                ConsoleUtils.WriteMessage("Shard Map Manager already exists");
            }
            else
            {
                // The Shard Map Manager does not exist, so create it
                shardMapManager = ShardMapManagerFactory.CreateSqlShardMapManager(shardMapManagerConnectionString);
                ConsoleUtils.WriteMessage("Created Shard Map Manager");
            }

            return shardMapManager;
        }

        /// <summary>
        /// Creates a new Range Shard Map with the specified name, or gets the Range Shard Map if it already exists.
        /// </summary>
        public static RangeShardMap<T> CreateOrGetRangeShardMap<T>(ShardMapManager shardMapManager, string shardMapName)
        {
            // Try to get a reference to the Shard Map.
            RangeShardMap<T> shardMap;
            bool shardMapExists = shardMapManager.TryGetRangeShardMap(shardMapName, out shardMap);

            if (shardMapExists)
            {
                ConsoleUtils.WriteMessage("Shard Map '{0}' already exists", shardMap.Name);
            }
            else
            {
                // The Shard Map does not exist, so create it
                shardMap = shardMapManager.CreateRangeShardMap<T>(shardMapName);
                ConsoleUtils.WriteMessage("Created Shard Map '{0}'", shardMap.Name);
            }

            return shardMap;
        }

        /// <summary>
        /// Creates a new Range Shard Map with the specified name, or gets the Range Shard Map if it already exists.
        /// </summary>
        public static ListShardMap<T> CreateOrGetListShardMap<T>(ShardMapManager shardMapManager, string shardMapName)
        {
            // Try to get a reference to the Shard Map.
            ListShardMap<T> shardMap;
            bool shardMapExists = shardMapManager.TryGetListShardMap(shardMapName, out shardMap);

            if (shardMapExists)
            {
                ConsoleUtils.WriteMessage("Shard Map '{0}' already exists", shardMap.Name);
            }
            else
            {
                // The Shard Map does not exist, so create it
                shardMap = shardMapManager.CreateListShardMap<T>(shardMapName);
                ConsoleUtils.WriteMessage("Created Shard Map '{0}'", shardMap.Name);
            }

            return shardMap;
        }

        /// <summary>
        /// Returns them if they have already been added.
        /// </summary>
        public static Shard GetShard(ShardMap shardMap, ShardLocation shardLocation)
        {
            ConsoleUtils.WriteMessage("Checking if shard '{0}' is registered with the shard map manager...", shardLocation);

            // Try to get a reference to the Shard
            Shard shard;
            bool shardExists = shardMap.TryGetShard(shardLocation, out shard);

            if (!shardExists)
            {
                ConsoleUtils.WriteMessage("Shard '{0}' doesn't exist.", shardLocation.Database);
            }

            return shard;
        }

        /// <summary>
        /// Adds Shards to the Shard Map
        /// </summary>
        public static Shard CreateShard(ShardMap shardMap, ShardLocation shardLocation)
        {
            ConsoleUtils.WriteMessage("Checking if shard '{0}' is registered with the shard map manager...", shardLocation);

            // Try to get a reference to the Shard
            Shard shard;
            bool shardExists = shardMap.TryGetShard(shardLocation, out shard);

            if (shardExists)
            {
                ConsoleUtils.WriteWarning("Shard '{0}' has already been added to the Shard Map", shardLocation.Database);
                return null;
            }
            else
            {
                // The Shard Map does not exist, so create it
                shard = shardMap.CreateShard(shardLocation);
                ConsoleUtils.WriteSuccess("Added shard '{0}' to the Shard Map", shardLocation.Database);
                return shard;
            }
        }

        /// <summary>
        /// Gets the shard map, if it exists. If it doesn't exist, writes out the reason and returns null.
        /// </summary>
        public static ListShardMap<int> TryGetShardMapListings(ShardMapManager shardMapManager)
        {
            if (shardMapManager == null)
            {
                ConsoleUtils.WriteWarning("Shard Map Manager has not yet been created");
                return null;
            }

            ListShardMap<int> shardMap;
            bool mapExists = shardMapManager.TryGetListShardMap(ConfigurationUtils.ShardMapName, out shardMap);

            if (!mapExists)
            {
                ConsoleUtils.WriteWarning("Shard Map Manager has been created, but the Shard Map has not been created");
                return null;
            }

            return shardMap;
        }

        /// <summary>
        /// Gets the shard map, if it exists. If it doesn't exist, writes out the reason and returns null.
        /// </summary>
        /// <param name="shardMap"></param>
        /// <param name="shardLocation"></param>
        public static void DeleteShard(ShardMap shardMap, ShardLocation shardLocation)
        {
            // Try to get a reference to the Shard
            Shard shard;
            bool shardExists = shardMap.TryGetShard(shardLocation, out shard);

            if (shardExists)
            {
                shardMap.DeleteShard(shard);

                ConsoleUtils.WriteSuccess("Deleted shard '{0}' from the Shard Map", shardLocation.Database);
            }
        }

        /// <summary>
        /// Appy the SQL script to the shard.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sqlScriptFile"></param>
        public static void ApplySqlSchemaToShard(string database, string sqlScriptFile)
        {
            // Apple sql script to shard. The script must be idempotent, in case it was already run on this database
            // and we failed to add it to the shard map previously
            SqlDatabaseUtils.ExecuteSqlScript(ConfigurationUtils.ShardMapManagerServerName, database, sqlScriptFile);

            ConsoleUtils.WriteSuccess("SQL script successfully applied to shard '{0}' from the Shard Map", database);
        }

        // could be more useful for getting shard details
        //public PointMapping<TKey> GetMappingForKey(TKey key)
        //{
        //    return GetMappingForKey(key, LookupOptions.LookupInStore);
        //}
    }
}
