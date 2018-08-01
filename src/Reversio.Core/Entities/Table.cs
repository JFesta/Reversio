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
    public class Table
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                SetIdentifier();
            }
        }

        private string _schema;
        public string Schema
        {
            get
            {
                return _schema;
            }
            set
            {
                _schema = value;
                SetIdentifier();
            }
        }

        public string Catalog { get; set; }
        public string Type { get; set; }

        public string Identifier { get; private set; }

        public IDictionary<string, Column> Columns { get; set; }
        public List<Index> Indices { get; set; }
        public Index Pk { get; set; }
        public List<ForeignKey> Dependencies { get; set; }
        public List<ForeignKey> DependenciesFrom { get; set; }

        public Poco Poco { get; set; }
        
        private void SetIdentifier()
        {
            Identifier = _schema + "." + _name;
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}
