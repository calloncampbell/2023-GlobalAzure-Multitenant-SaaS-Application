using System;
using System.Linq;

namespace ElasticShardSqlUtil.Options
{
    public class ShardOptions
    {
        public int[] Tenants { get; set; }

        public string TenantType { get; set; }
        
        public string DatabaseName { get; set; }

        public string File { get; set; }
        
        public ShardOptions(int[] tenants, string tenantType, string databaseName, string file)
        {
            Tenants = tenants;
            TenantType = tenantType;
            DatabaseName = databaseName;
            File = file;
        }
    }
}
