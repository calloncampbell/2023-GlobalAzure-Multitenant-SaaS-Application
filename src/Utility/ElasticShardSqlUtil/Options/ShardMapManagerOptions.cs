using System;
using System.Linq;

namespace ElasticShardSqlUtil.Options
{
    public enum ShardMapManagerActions
    {
        Create,
        Status
    }
    
    public class ShardMapManagerOptions
    {
        public ShardMapManagerActions Action { get; set; }

        public ShardMapManagerOptions()
        {
            
        }
    }
}
