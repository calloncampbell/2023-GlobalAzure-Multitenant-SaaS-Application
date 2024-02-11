using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement.Recovery;
using System;
using System.Linq;

namespace ElasticShardSqlUtil.Options
{
    public enum ShardMapManagerActions
    {
        Create,
        Status,
        Cleanup
    }
    
    public class ShardMapManagerOptions
    {
        public ShardMapManagerActions Action { get; set; }

        public ShardMapManagerOptions()
        {
            
        }
    }
}
