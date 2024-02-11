using ElasticShardSqlUtil.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.Linq;

namespace ElasticShardSqlUtil
{
    public static class ConfigurationUtils
    {
        public static IConfigurationRoot Config { get; set; }
        public static IConfigurationBuilder ConfigurationBuilder { get; set; }

        /// <summary>
        /// The name of the shard map manager server.
        /// </summary>
        public static void InitializeConfiguration()
        {
            ConfigurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Config = ConfigurationBuilder.Build();
        }

        /// <summary>
        /// Gets the server name for the Shard Map Manager database, which contains the shard maps.
        /// </summary>
        public static string ShardMapManagerServerName
        {
            get { return ServerName; }
        }

        /// <summary>
        /// Gets the database name for the Shard Map Manager database, which contains the shard maps.
        /// </summary>
        public static string ShardMapManagerDatabaseName
        {
            get { return Config["ShardMapManagerDatabaseName"]; }
        }

        /// <summary>
        /// Gets the name for the Shard Map that contains metadata for all the shards and the mappings to those shards.
        /// </summary>
        public static string ShardMapName
        {
            get { return Config["ShardMapName"]; }
        }

        /// <summary>
        /// Gets the server name from the App.config file for shards to be created on.
        /// </summary>
        private static string ServerName
        {
            get { return Config["DatabaseServerName"]; }
        }

        /// <summary>
        /// Gets the connection string for the specified server and database.
        /// </summary>
        public static string ShardDatabaseNameFormat
        {
            get { return Config["ShardDatabaseNameFormat"]; }
        }

        /// <summary>
        /// Returns a connection string that can be used to connect to the specified server and database.
        /// </summary>
        public static string GetConnectionString(string serverName, string database)
        {
            SqlConnectionStringBuilder connStr = new SqlConnectionStringBuilder(GetCredentialsConnectionString());
            connStr.DataSource = serverName;
            connStr.InitialCatalog = database;
            return connStr.ToString();
        }

        /// <summary>
        /// Returns a connection string to use for Data-Dependent Routing and Multi-Shard Query,
        /// which does not contain DataSource or InitialCatalog.
        /// </summary>
        public static string GetCredentialsConnectionString()
        {
            // Get Username and password from the appsettings.json file. If they don't exist, default to string.Empty.
            string userId = Config["SqlUsername"] ?? string.Empty;
            string password = Config["SqlPassword"] ?? string.Empty;

            // Get Integrated Security from the app.config file. 
            // If it exists, then parse it (throw exception on failure), otherwise default to false.
            string integratedSecurityString = Config["IntegratedSecurity"];
            bool integratedSecurity = integratedSecurityString != null && bool.Parse(integratedSecurityString);

            SqlConnectionStringBuilder connStr = new SqlConnectionStringBuilder
            {
                // DDR and MSQ require credentials to be set
                UserID = userId,
                Password = password,
                IntegratedSecurity = integratedSecurity,

                // DataSource and InitialCatalog cannot be set for DDR and MSQ APIs, because these APIs will
                // determine the DataSource and InitialCatalog for you.
                //
                // DDR also does not support the ConnectRetryCount keyword introduced in .NET 4.5.1, because it
                // would prevent the API from being able to correctly kill connections when mappings are switched
                // offline.
                //
                // Other SqlClient ConnectionString keywords are supported.

                ApplicationName = "DniElasticSqlTool",
                ConnectTimeout = 30
            };
            return connStr.ToString();
        }

        /// <summary>
        /// Returns a connection string that can be used to connect to the specified server and database.
        /// </summary>
        /// <returns></returns>
        public static string[] GetDatabaseReferenceTables()
        {
            return Config.GetSection("DatabaseReferenceTables").GetChildren().Select(x => x.Value).ToArray();
        }

        /// <summary>
        /// Returns a connection string that can be used to connect to the specified server and database.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DatabaseShardTable> GetDatabaseShardTables()
        {
            return Config.GetSection("DatabaseShardTables").GetChildren().Select(x => new DatabaseShardTable
            {
                TableName = x["TableName"],
                KeyColumnName = x["KeyColumnName"]
            });
        }
    }
}
