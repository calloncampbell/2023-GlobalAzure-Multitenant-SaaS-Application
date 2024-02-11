using ElasticShardSqlUtil.Options;
using System;
using System.Linq;

namespace ElasticShardSqlUtil.Interfaces
{
    internal interface IRecoveryManagement
    {
        void RecoveryManagementAction(RecoveryOptions options);
    }
}
