using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Entities
{
    public class IndexColumn
    {
        public string IndexName { get; set; }
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public int Position { get; set; }
        public string ColumnName { get; set; }

        public Index Index { get; set; }
        public Column Column { get; set; }
    }
}
