using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Entities
{
    public class Index
    {
        public string IndexName { get; set; }
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public bool IsUnique { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool isUniqueConstraint { get; set; }

        public Table Table { get; set; }

        public Dictionary<int, IndexColumn> Columns { get; set; }

        public override string ToString()
        {
            return IndexName;
        }
    }
}
