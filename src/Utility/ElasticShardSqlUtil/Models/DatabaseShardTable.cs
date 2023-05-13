using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticShardSqlUtil.Models
{
    public class DatabaseShardTable
    {
        public string TableName { get; set; }
        public string KeyColumnName { get; set; }
    }
}
