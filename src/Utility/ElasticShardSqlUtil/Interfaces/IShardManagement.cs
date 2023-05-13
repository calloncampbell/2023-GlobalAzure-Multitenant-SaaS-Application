using ElasticShardSqlUtil.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticShardSqlUtil.Interfaces
{
    internal interface IShardManagement
    {
        void ShardMapManagerAction(ShardMapManagerOptions options);        
        void AddShardAction(ShardOptions options);
        void GetShardAction(ShardOptions options);
        void DeleteShardAction(ShardOptions options);
        void SqlScriptShardAction(ShardOptions options);
    }
}
