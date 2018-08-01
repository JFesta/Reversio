using Dapper;
using Reversio.Core.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.IO;
using Reversio.Core.Settings;
using Reversio.Core.Logging;

namespace Reversio.Core.SqlEngine
{
    public class SqlServerEngine : ISqlEngine
    {
        public static string Provider = "System.Data.SqlClient";

        private string _connectionString;
        private IEnumerable<Table> _context;

        public SqlServerEngine(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Table> Load(LoadStep settings)
        {
            if (_context == null)
                LoadFromDb();

            //bool filterSchemas = (settings.Schemas == null || settings.Schemas.Any(s => !String.IsNullOrWhiteSpace(s)));

            return _context
                .Where(e => TypeMatch(e, settings.EntityTypes))
                .Where(e => !(settings.Schemas != null && settings.Schemas.Any(s => !String.IsNullOrWhiteSpace(s))) || settings.Schemas.Contains(e.Schema.ToLowerInvariant()))
                .Where(e => !settings.Exclude.IsMatch(e.Schema, e.Name));
        }

        private void LoadFromDb()
        {
            IEnumerable<Column> columns;
            IEnumerable<ForeignKey> foreignKeys;
            IEnumerable<ForeignKeyColumn> foreignKeyColumns;
            IEnumerable<Index> indices;
            IEnumerable<IndexColumn> indexColumns;
            
            //load all
            Log.Debug("Connecting to SQL Server Database...");
            using (var connection = new SqlConnection(_connectionString))
            {
                _context = connection.Query<Table>(Queries.TABLES_SELECT);
                columns = connection.Query<Column>(Queries.COLUMNS_SELECT);
                foreignKeys = connection.Query<ForeignKey>(Queries.FOREIGN_KEYS_SELECT);
                foreignKeyColumns = connection.Query<ForeignKeyColumn>(Queries.FOREIGN_KEY_COLUMNS_SELECT);
                indices = connection.Query<Index>(Queries.INDICES_SELECT);
                indexColumns = connection.Query<IndexColumn>(Queries.INDEX_COLUMNS_SELECT);
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
                var fkTable = _context.FirstOrDefault(e => e.Schema == foreignKey.FkSchema && e.Name == foreignKey.FkTableName);
                var pkTable = _context.FirstOrDefault(e => e.Schema == foreignKey.PkSchema && e.Name == foreignKey.PkTableName);
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

                    foreignKey.Columns = foreignKeyColumns.Where(c => c.Schema == foreignKey.Schema && c.Name == foreignKey.Name)
                    .ToDictionary(c => c.Position);
                    foreach (var column in foreignKey.Columns.Values)
                    {
                        column.ForeignKey = foreignKey;
                        var fkTableColumn = fkTable.Columns.Values.FirstOrDefault(c => c.Name == column.FkColumnName);
                        if (fkTableColumn != null)
                        {
                            if (fkTableColumn.Dependencies == null)
                                fkTableColumn.Dependencies = new List<ForeignKeyColumn>();
                            fkTableColumn.Dependencies.Add(column);
                            column.FkColumn = fkTableColumn;
                        }

                        var pkTableColumn = pkTable.Columns.Values.FirstOrDefault(c => c.Name == column.PkColumnName);
                        if (pkTableColumn != null)
                        {
                            if (pkTableColumn.DependenciesFrom == null)
                                pkTableColumn.DependenciesFrom = new List<ForeignKeyColumn>();
                            pkTableColumn.DependenciesFrom.Add(column);
                            column.PkColumn = pkTableColumn;
                        }
                    }
                }

                Log.Debug(String.Format("{0} [{1}].[{2}]: from {3} to {4} - {5} columns",
                    "FK", foreignKey.Schema, foreignKey.Name,
                    fkTable?.Identifier, pkTable?.Identifier,
                    foreignKey.Columns == null ? 0 : foreignKey.Columns.Count));
            }

            //linking indices + index columns
            foreach (var index in indices)
            {
                var table = _context.FirstOrDefault(e => e.Schema == index.TableSchema && e.Name == index.TableName);
                if (table != null)
                {
                    index.Table = table;
                    if (table.Indices == null)
                        table.Indices = new List<Index>();
                    table.Indices.Add(index);
                    if (index.IsPrimaryKey)
                        table.Pk = index;

                    index.Columns = indexColumns.Where(c => c.TableSchema == index.TableSchema && c.TableName == index.TableName && c.IndexName == index.IndexName)
                        .ToDictionary(c => c.Position);
                    foreach (var column in index.Columns.Values)
                    {
                        column.Index = index;
                        var tableColumn = table.Columns.Values.FirstOrDefault(c => c.Name == column.ColumnName);
                        if (tableColumn != null)
                        {
                            if (tableColumn.IndexColumns == null)
                                tableColumn.IndexColumns = new List<IndexColumn>();
                            tableColumn.IndexColumns.Add(column);
                            column.Column = tableColumn;
                        }
                    }
                    Log.Debug(String.Format("{0} {1}: {2} {3} - on table [{4}].[{5}] - {6} columns",
                        "INDEX", index.IndexName, 
                        index.IsPrimaryKey ? "PK" : String.Empty,
                        index.IsUnique ? "UNIQUE" : String.Empty,
                        index.TableSchema, index.TableName,
                        index.Columns == null ? 0 : index.Columns.Count));
                }
            }
        }

        public string GetCSharpType(Column column, bool nullableIfDefaultAndNotPk)
        {
            switch (column.DataType.ToLower())
            {
                case "bit":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column))) ? "bool?" : "bool";
                case "tinyint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "byte?" : "byte";
                case "smallint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "short?" : "short";
                case "int":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "int?" : "int";
                case "bigint":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "long?" : "long";
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "decimal?" : "decimal";
                case "float":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "float?" : "float";
                case "real":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "double?" : "double";
                case "datetime":
                case "datetime2":
                case "date":
                case "smalldatetime":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "DateTime?" : "DateTime";
                case "time":
                case "datetimeoffset":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "TimeSpan?" : "TimeSpan";
                case "char":
                case "varchar":
                case "text":
                case "nchar":
                case "ntext":
                case "nvarchar":
                    return "string";
                case "binary":
                case "varbinary":
                case "image":
                    return "byte[]";
                case "xml":
                    return "string";
                case "uniqueidentifier":
                    return column.IsNullable || (nullableIfDefaultAndNotPk && HasDefaultAndNotPk(column)) ? "Guid?" : "Guid";
                case "timestamp":
                    return "byte[]";
                default:
                    throw new Exception("Can't parse column type: " + column.DataType);
            }
        }

        private bool HasDefaultAndNotPk(Column column)
        {
            return !String.IsNullOrWhiteSpace(column.Default)
                && (column.Table.Pk == null || !column.Table.Pk.Columns.ContainsKey(column.Position));
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

        public bool IsString(Column column)
        {
            switch (column.DataType.ToLower())
            {
                case "char":
                case "varchar":
                case "text":
                case "nchar":
                case "ntext":
                case "nvarchar":
                    return true;
            }
            return false;
        }

        public bool IsUnicode(Column column)
        {
            switch (column.DataType.ToLower())
            {
                case "nchar":
                case "ntext":
                case "nvarchar":
                    return true;
            }
            return false;
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
    }
}
