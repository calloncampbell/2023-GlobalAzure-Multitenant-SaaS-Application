using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace ApiFunctionApp.Entities.Shards
{
    public class Shard
    {
        private ShardMapManager _shardMapManager;        
        private readonly IConfiguration _configuration;
        private readonly string _connectionString = "Data Source=gab-labb-cnc-saas-sql.database.windows.net;Initial Catalog=gab-labb-cnc-saas-tenant-catalog-sqldb;Integrated Security=False;User ID=sqladmin;Password=P@ssw0rd1;Connect Timeout=30;Application Name=ShardApiFunctionApp";
        private readonly string _connectionStringShard = "Integrated Security=False;User ID=sqladmin;Password=P@ssw0rd1;Connect Timeout=30;Application Name=ShardApiFunctionApp";

        public ListShardMap<int> ShardMap { get; private set; }
        
        public Shard(IConfiguration configuration)
        {
            _configuration = configuration;
            SqlConnectionStringBuilder masterShardConnectionBuilder = new SqlConnectionStringBuilder
            {
                DataSource = _configuration["ShardDatabaseServerName"],
                IntegratedSecurity = false,
                UserID = _configuration["ShardSqlUser"],
                Password = _configuration["ShardSqlPassword"],
                InitialCatalog = _configuration["ShardManagerDb"],
                ApplicationName = "ShardApiFunctionApp"
            };
            
            SqlConnectionStringBuilder shardConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                IntegratedSecurity = false,
                UserID = _configuration["ShardSqlUser"],
                Password = _configuration["ShardSqlPassword"],
                ApplicationName = "ShardApiFunctionApp"
            };
            
            _connectionString = masterShardConnectionBuilder.ToString();
            _connectionStringShard = shardConnectionStringBuilder.ToString();

            var shardMapName = _configuration["ShardMapName"];

             ShardMapManager smm;
            if (!ShardMapManagerFactory.TryGetSqlShardMapManager(_connectionString, ShardMapManagerLoadPolicy.Lazy, out smm))
            {
                _shardMapManager = ShardMapManagerFactory.CreateSqlShardMapManager(_connectionString);
            }
            else
            {
                _shardMapManager = smm;
            }

            ListShardMap<int> sm;
            if (!_shardMapManager.TryGetListShardMap<int>(shardMapName, out sm))
            {
                ShardMap = _shardMapManager.CreateListShardMap<int>(shardMapName);
            }
            else
            {
                ShardMap = sm;
            }
        }
        
        public async Task<SqlConnection> OpenConnectionAsync(int key)
        {
            return await ShardMap.OpenConnectionForKeyAsync(key, _connectionStringShard);
        }
    }
}
