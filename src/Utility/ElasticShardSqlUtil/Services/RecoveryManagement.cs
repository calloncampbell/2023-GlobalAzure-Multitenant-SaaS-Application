using ElasticShardSqlUtil.Interfaces;
using ElasticShardSqlUtil.Options;
using ElasticShardSqlUtil.Utils;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement.Recovery;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using System;
using System.Diagnostics;
using System.Linq;

namespace ElasticShardSqlUtil.Services
{
    internal class RecoveryManagement : IRecoveryManagement
    {
        /// <summary>
        /// The shard map manager, or null if it does not exist. 
        /// It is recommended that you keep only one shard map manager instance in
        /// memory per AppDomain so that the mapping cache is not duplicated.
        /// </summary>
        static ShardMapManager s_shardMapManager;

        /// <summary>
        /// The recovery manager, or null if it does not exist.
        /// </summary>
        static RecoveryManager s_recoveryManager;

        public RecoveryManagement()
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

            s_recoveryManager = s_shardMapManager.GetRecoveryManager();
        }

        /// <summary>
        /// Creates a new shard map manager database and shard map.
        /// </summary>
        /// <param name="options"></param>
        public void RecoveryManagementAction(RecoveryOptions options)
        {
            if (options.Action == RecoveryOptionActions.Detach)
            {
                DetachShard(options);
            }
            else if (options.Action == RecoveryOptionActions.Detect)
            {
                DetectMappingDifferences(options);
            }
            else if (options.Action == RecoveryOptionActions.Resolve)
            {
                ResolveMappingDifferences(options);
            }
            else if (options.Action == RecoveryOptionActions.Attach)
            {
                AttachShard(options);
            }
            else
            {
                ConsoleUtils.WriteWarning($"Recovery Manager action '{options.Action}' is not supported.");
            }
        }

        /// <summary>
        /// The DetachShard method detaches the given shard from the shard map and deletes mappings associated with the shard.
        /// IMPORTANT: Use this technique only if you are certain that the range for the updated mapping is empty. This method does not check data for the range being moved, so it is best to include checks in your code.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        private void DetachShard(RecoveryOptions options)
        {
            if (options.Tenants.Length == 0)
            {
                throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            }

            ConsoleUtils.WriteWarning("WARNING: This action will detach the shard from the shard map manager and delete all mappings associated with the shard. Use this technique only if you are certain that the range for the updated mapping is empty. Do you want to proceed? (Y/N)");
            var userInput = Console.ReadLine();
            if (userInput.ToLower() != "y")
            {
                ConsoleUtils.WriteWarning("Action has been aborted.");
                return;
            }

            // Get the shard map
            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap is null)
            {
                return;
            }

            foreach (var id in options.Tenants)
            {
                ConsoleUtils.WriteInfo($"Processing tenant '{id}' for detachment from shard map manager...");

                var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
                if (pointMapping != null && pointMapping.Value == id)
                {
                    Console.WriteLine("Tenant '{0}' status is '{1}' and is mapped to server '{2}', database '{3}'. Proceeding with detachment...", pointMapping.Value, pointMapping.Status.ToString(), pointMapping.Shard.Location.Server, pointMapping.Shard.Location.Database);

                    ShardLocation shardLocation = pointMapping.Shard.Location;
                    var databaseName = pointMapping.Shard.Location.Database;

                    // Detach the shard
                    s_recoveryManager.DetachShard(shardLocation);

                    ConsoleUtils.WriteMessage("Shard location {0} has been detached from shard map manager", databaseName);
                }
                else
                {
                    ConsoleUtils.WriteWarning($"Tenant '{id}' doesn't exist in the shard map manager.");
                }
            }
        }

        /// <summary>
        /// The DetectMappingDifferences method selects and returns one of the shard maps (either local or global) as the source of truth and reconciles mappings on both shard maps (GSM and LSM).
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        private void DetectMappingDifferences(RecoveryOptions options)
        {
            if (options.Tenants.Length == 0)
            {
                throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            }

            // Get the shard map
            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap is null)
            {
                return;
            }

            foreach (var id in options.Tenants)
            {
                ConsoleUtils.WriteInfo($"Processing tenant '{id}' for detachment from shard map manager...");

                var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
                if (pointMapping != null && pointMapping.Value == id)
                {
                    Console.WriteLine("Tenant '{0}' status is '{1}' and is mapped to server '{2}', database '{3}'. Proceeding with detachment...", pointMapping.Value, pointMapping.Status.ToString(), pointMapping.Shard.Location.Server, pointMapping.Shard.Location.Database);

                    ShardLocation shardLocation = pointMapping.Shard.Location;

                    // Detect mapping differences
                    var mappingDifferences = s_recoveryManager.DetectMappingDifferences(shardLocation);

                    ConsoleUtils.WriteInfo($"Mapping differences detected: {mappingDifferences.Count()}");
                    foreach (var mappingDifference in mappingDifferences)
                    {
                        ConsoleUtils.WriteInfo($"Mapping difference detected for Recovery Token: {mappingDifference.ToString()}");
                    }
                }
                else
                {
                    ConsoleUtils.WriteWarning($"Tenant '{id}' doesn't exist in the shard map manager.");
                }
            }
        }

        /// <summary>
        /// he ResolveMappingDifferences method selects one of the shard maps (either local or global) as the source of truth and reconciles mappings on both shard maps (GSM and LSM).
        /// </summary>
        /// <param name="options"></param>
        private void ResolveMappingDifferences(RecoveryOptions options)
        {
            if (options.Tenants.Length == 0)
            {
                throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            }

            // Get the shard map
            ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            if (shardMap is null)
            {
                return;
            }

            // Get all mappings, grouped by the shard that they are on. We do this all in one go to minimise round trips.
            ILookup<Shard, PointMapping<int>> mappingsGroupedByShard = shardMap.GetMappings().ToLookup(m => m.Shard);

            foreach (var id in options.Tenants)
            {
                ConsoleUtils.WriteInfo($"Processing tenant '{id}' for detachment from shard map manager...");

                // Get the mappings for this shard
                var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
                if (pointMapping != null && pointMapping.Value == id)
                {
                    Console.WriteLine("Tenant '{0}' status is '{1}' and is mapped to server '{2}', database '{3}'. Proceeding with resolving mappings if any...", pointMapping.Value, pointMapping.Status.ToString(), pointMapping.Shard.Location.Server, pointMapping.Shard.Location.Database);

                    // Get the shard location
                    ShardLocation shardLocation = pointMapping.Shard.Location;

                    // Detect mapping differences
                    var mappingDifferences = s_recoveryManager.DetectMappingDifferences(shardLocation);

                    ConsoleUtils.WriteInfo($"Mapping differences detected: {mappingDifferences.Count()}");
                    foreach (var mappingDifference in mappingDifferences)
                    {
                        ConsoleUtils.WriteInfo($"Resolving mapping difference for Recovery Token: {mappingDifference.ToString()}");

                        // Resolve mapping differences
                        s_recoveryManager.ResolveMappingDifferences(mappingDifference, options.MappingDifferenceResolutionType);
                    }
                }
                else
                {
                    ConsoleUtils.WriteWarning($"Tenant '{id}' doesn't exist in the shard map manager.");
                }
            }           
        }

        /// <summary>
        /// The AttachShard method attaches the given shard to the shard map. It then detects any shard map inconsistencies and updates the mappings to match the shard at the point of the shard restoration. It is assumed that the database is also renamed to reflect the original database name (before the shard was restored), since the point-in time restoration defaults to a new database appended with the timestamp.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        private void AttachShard(RecoveryOptions options)
        {
            throw new NotImplementedException();

            //if (options.Tenants.Length == 0)
            //{
            //    throw new Exception("Missing command line argument for tenants. Try again with format '100 101 102'");
            //}

            //// Get the shard map
            //ListShardMap<int> shardMap = ShardManagementUtils.TryGetShardMapListings(s_shardMapManager);
            //if (shardMap is null)
            //{
            //    return;
            //}

            //foreach (var id in options.Tenants)
            //{
            //    ConsoleUtils.WriteInfo($"Processing tenant '{id}' for attachment to shard map manager...");

            //    var pointMapping = shardMap.GetMappings().FirstOrDefault(m => m.Value == id);
            //    if (pointMapping != null && pointMapping.Value == id)
            //    {
            //        ConsoleUtils.WriteWarning($"Tenant '{id}' already exists in the shard map manager.");
            //    }
            //    else
            //    {
            //        ConsoleUtils.WriteInfo($"Tenant '{id}' doesn't exist in the shard map manager. Proceeding with attachment...");

            //        // TODO: Attach the shard
            //        ShardLocation shardLocation = new ShardLocation(ConfigurationUtils.ShardServerName, ConfigurationUtils.ShardDatabaseName);
            //        s_recoveryManager.AttachShard(shardLocation);

            //        //// TODO: Review if I need to: Add the mapping
            //        //pointMapping = shardMap.CreatePointMapping(id, shardLocation);
            //        //shardMap.AddMapping(pointMapping);

            //        ConsoleUtils.WriteMessage($"Tenant '{id}' has been attached to shard map manager.");
            //    }
            //}
        }
    }
}
