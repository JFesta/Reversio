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
        public bool IsIdentity { get; set; }
        public bool IsNullable { get; set; }
        public string DataType { get; set; }
        public int? CharacterMaximumLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public int? DateTimePrecision { get; set; }
        public bool GeneratedAlwaysType { get; set; }
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
