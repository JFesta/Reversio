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

using Dapper;
using Reversio.Core.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Text;
using System.IO;
using Reversio.Core.Settings;
using Reversio.Core.Logging;
using System.Data.SqlClient;

namespace Reversio.Core.SqlEngine
{
    public class MySqlEngine : ISqlEngine
    {
        public static string Provider = "MySql.Data.MySqlClient";

        private string _connectionString;
        private IEnumerable<Table> _context;

        public MySqlEngine(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Table> Load(LoadStep settings)
        {
            if (_context == null)
                LoadFromDb();

            return _context
                .Where(e => TypeMatch(e, settings.EntityTypes))
                .Where(e => !(settings.Schemas != null && settings.Schemas.Any(s => !String.IsNullOrWhiteSpace(s))) || settings.Schemas.Contains(e.Schema.ToLowerInvariant()))
                .Where(e => !settings.Exclude.IsMatch(e.Schema, e.Name));
        }

        private void LoadFromDb()
        {
            IEnumerable<Column> columns;

            //load all
            Log.Debug("Connecting to MySql Database...");
            using (var connection = new MySqlConnection(_connectionString))
            {
                _context = connection.Query<Table>(Queries.MYSQL_TABLES_SELECT);
                columns = connection.Query<Column>(Queries.MYSQL_COLUMNS_SELECT);
            }

            //linking entities + columns
            foreach (var entity in _context)
            {
                entity.Columns = columns.Where(c => c.Schema == entity.Schema && c.TableName == entity.Name).ToDictionary(c => c.Name);
                foreach (var column in entity.Columns.Values)
                {
                    column.Table = entity;
                }
                Log.Debug(String.Format("{0} [{1}].[{2}]: {3} columns", entity.Type, entity.Schema, entity.Name, entity.Columns == null ? 0 : entity.Columns.Count));
            }
        }

        public string GetCSharpType(Column column, bool nullableIfDefaultAndNotPk)
        {
            switch (column.DataType.ToLower())
            {
                case "tinyint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "byte?" : "byte";
                case "smallint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "short?" : "short";
                case "int":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "int?" : "int";
                case "bigint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "long?" : "long";
                default:
                    throw new Exception("Can't parse column type: " + column.DataType);
            }
        }

        public string ForeignKeyRuleString(string rule)
        {
            return null;
        }
        
        public string GetFullName(Table table)
        {
            throw new NotImplementedException();
        }

        public string GetIdentitySpecifier(Column column)
        {
            throw new NotImplementedException();
        }

        public bool IsString(Column column)
        {
            throw new NotImplementedException();
        }

        public bool IsUnicode(Column column)
        {
            throw new NotImplementedException();
        }

        public bool IsView(Table entity)
        {
            throw new NotImplementedException();
        }

        public bool TypeMatch(Table table, IEnumerable<string> allowedTypes)
        {
            return true;
        }

        private bool HasDefaultAndNotPk(Column column)
        {
            return true;
        }
    }
}
