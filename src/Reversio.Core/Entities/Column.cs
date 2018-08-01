using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Entities
{
    public class Column
    {
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public string Default { get; set; }
        public bool IsNullable { get; set; }
        public string DataType { get; set; }
        public int? CharacterMaximumLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public int? DateTimePrecision { get; set; }

        public Table Table { get; set; }
        public List<ForeignKeyColumn> Dependencies { get; set; }
        public List<ForeignKeyColumn> DependenciesFrom { get; set; }
        public List<IndexColumn> IndexColumns { get; set; }

        public PocoBaseProperty PocoProperty { get; set; }
        
        public override string ToString()
        {
            return Name;
        }
    }
}
