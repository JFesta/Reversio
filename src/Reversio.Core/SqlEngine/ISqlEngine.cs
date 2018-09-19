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

using Reversio.Core.Entities;
using Reversio.Core.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.SqlEngine
{
    public interface ISqlEngine
    {
        IEnumerable<Table> Load(LoadStep settings);

        string GetCSharpType(Column column, bool nullableIfDefaultAndNotPk);
        bool IsView(Table entity);
        bool IsString(Column column);
        bool IsUnicode(Column column);
        string ForeignKeyRuleString(string rule);
        string GetFullName(Table table);
    }
}
