using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reversio.Core.Entities
{
    public class ForeignKey
    {
        public string Schema { get; set; }
        public string Name { get; set; }

        public string FkSchema { get; set; }
        public string FkTableName { get; set; }

        public string PkSchema { get; set; }
        public string PkTableName { get; set; }
        
        public string UpdateRuleStr { get; set; }
        public string DeleteRuleStr { get; set; }

        //public ForeignKeyRule UpdateRule { get; set; }
        //public ForeignKeyRule DeleteRule { get; set; }

        public Table FkTable { get; set; }
        public Table PkTable { get; set; }
        public Dictionary<int, ForeignKeyColumn> Columns { get; set; }

        public bool IsOne()
        {
            return (PkTable.Pk != null && FkTable.Pk != null
                && PkTable.Pk.Columns.Values.All(c => Columns.Values.Any(fkc => c.Column.Name.Equals(fkc.PkColumn.Name)))
                //&& Columns.Values.All(fkc => PkTable.Pk.Columns.Values.Any(c => c.Column.Name.Equals(fkc.FkColumn.Name)))
                && FkTable.Pk.Columns.Values.All(c => Columns.Values.Any(fkc => c.Column.Name.Equals(fkc.FkColumn.Name))));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
