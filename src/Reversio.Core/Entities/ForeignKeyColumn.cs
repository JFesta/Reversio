using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Entities
{
    public class ForeignKeyColumn
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }

        public string FkSchemaName { get; set; }
        public string FkTableName { get; set; }
        public string FkColumnName { get; set; }

        public string PkSchemaName { get; set; }
        public string PkTableName { get; set; }
        public string PkColumnName { get; set; }

        public ForeignKey ForeignKey { get; set; }
        public Column FkColumn { get; set; }
        public Column PkColumn { get; set; }
    }
}
