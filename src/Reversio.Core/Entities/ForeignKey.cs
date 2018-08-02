/*
 * Copyright 2018 Jacopo Festa 
 * This file is part of Reversio.
 * Reversio is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Reversio is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License
 * along with Reversio. If not, see <http://www.gnu.org/licenses/>.
*/

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

        public string FkSchemaName { get; set; }
        public string FkTableName { get; set; }

        public string PkSchemaName { get; set; }
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
