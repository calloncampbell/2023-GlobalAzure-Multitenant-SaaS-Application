using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement.Recovery;
using System;
using System.Linq;

namespace ElasticShardSqlUtil.Options
{
    public enum RecoveryOptionActions
    {
        Attach,
        Detach,
        Detect,
        Resolve
    }

    public class RecoveryOptions
    {
        public int[] Tenants { get; set; }

        public RecoveryOptionActions Action { get; set; }

        public MappingDifferenceResolution MappingDifferenceResolutionType { get; set; }

        public RecoveryOptions(int[] tenants, RecoveryOptionActions action, MappingDifferenceResolution mappingDifferenceResolutionType = MappingDifferenceResolution.KeepShardMapping)
        {
            Tenants = tenants;
            Action = action;
            MappingDifferenceResolutionType = mappingDifferenceResolutionType;
        }
    }
}
