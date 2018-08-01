﻿/*
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
