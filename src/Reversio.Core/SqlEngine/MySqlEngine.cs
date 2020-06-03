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
            IEnumerable<ForeignKey> foreignKeys;
            //IEnumerable<ForeignKeyColumn> foreignKeyColumns;

            //load all
            Log.Debug("Connecting to MySql Database...");
            using (var connection = new MySqlConnection(_connectionString))
            {
                _context = connection.Query<Table>(Queries.MYSQL_TABLES_SELECT);
                columns = connection.Query<Column>(Queries.MYSQL_COLUMNS_SELECT);
                foreignKeys = connection.Query<ForeignKey>(Queries.MYSQL_FOREIGN_KEYS_SELECT);
                //foreignKeyColumns = connection.Query<ForeignKeyColumn>(Queries.FOREIGN_KEY_COLUMNS_SELECT);
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

            //linking foreign keys + foreign key columns
            foreach (var foreignKey in foreignKeys)
            {
                var fkTable = _context.FirstOrDefault(e => e.Schema == foreignKey.FkSchemaName && e.Name == foreignKey.FkTableName);
                var pkTable = _context.FirstOrDefault(e => e.Schema == foreignKey.PkSchemaName && e.Name == foreignKey.PkTableName);
                if (fkTable != null && pkTable != null)
                {
                    foreignKey.FkTable = fkTable;
                    if (fkTable.Dependencies == null)
                        fkTable.Dependencies = new List<ForeignKey>();
                    fkTable.Dependencies.Add(foreignKey);

                    foreignKey.PkTable = pkTable;
                    if (pkTable.DependenciesFrom == null)
                        pkTable.DependenciesFrom = new List<ForeignKey>();
                    pkTable.DependenciesFrom.Add(foreignKey);

                    //TODO: column specifications for DbContext generation
                }

                Log.Debug(String.Format("{0} [{1}].[{2}]: from {3} to {4} - {5} columns",
                    "FK", foreignKey.Schema, foreignKey.Name,
                    fkTable?.Identifier, pkTable?.Identifier,
                    foreignKey.Columns == null ? 0 : foreignKey.Columns.Count));
            }
        }

        public string GetCSharpType(Column column, bool nullableIfDefaultAndNotPk)
        {
            switch (column.DataType.ToLower())
            {
                case "tinyint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "bool?" : "bool";
                case "smallint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "short?" : "short";
                case "int":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "int?" : "int";
                case "bigint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "long?" : "long";
                case "decimal":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "decimal?" : "decimal";
                case "date":
                case "datetime":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "DateTime?" : "DateTime";
                case "time":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "TimeSpan?" : "TimeSpan";
                case "char":
                    return (column.CharacterMaximumLength == 36)
                        ? (column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "Guid?" : "Guid")
                        : "string";
                case "varchar":
                case "text":
                case "json":
                    return "string";
                default:
                    throw new Exception("Can't parse column type: " + column.DataType);
            }
        }

        public string ForeignKeyRuleString(string rule)
        {
            switch (rule)
            {
                case ("NO ACTION"):
                    return "DeleteBehavior.ClientSetNull";
                case ("CASCADE"):
                    return "DeleteBehavior.Cascade";
                case ("SET NULL"):
                    return "DeleteBehavior.SetNull";
                case ("SET DEFAULT"):
                    return "DeleteBehavior.SetNull";
                default:
                    return null;
            }
        }
        
        public string GetFullName(Table table)
        {
            return String.Format("`{0}`.`{1}`", table.Schema, table.Name);
        }

        public string GetIdentitySpecifier(Column column)
        {
            return "";
        }

        public bool IsString(Column column)
        {
            switch (column.DataType.ToLower())
            {
                case "char":
                case "varchar":
                case "text":
                    return true;
            }
            return false;
        }

        public bool IsUnicode(Column column)
        {
            return true;
        }

        public bool TypeMatch(Table table, IEnumerable<string> allowedTypes)
        {
            return (IsView(table))
                ? allowedTypes.Contains("view")
                : allowedTypes.Contains("table");
        }

        public bool IsView(Table entity)
        {
            return String.Equals(entity.Type, "VIEW", StringComparison.InvariantCultureIgnoreCase);
        }

        private bool HasDefaultAndNotPk(Column column)
        {
            return !String.IsNullOrWhiteSpace(column.Default)
                && (column.Table.Pk == null || !column.Table.Pk.Columns.ContainsKey(column.Position));
        }
    }
}
