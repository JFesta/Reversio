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

using Reversio.Core.Settings;
using Reversio.Core.SqlEngine;
using Reversio.Core.Utils;
using Reversio.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reversio.Core
{
    public class DbContextEngine
    {
        private GlobalSettings _globalSettings;
        private DbContextStep _settings;
        private ISqlEngine _sqlEngine;

        public DbContextEngine(GlobalSettings globalSettings, DbContextStep settings, ISqlEngine sqlEngine)
        {
            _globalSettings = globalSettings;
            _settings = settings;
            _sqlEngine = sqlEngine;
        }

        public void WriteDbContext(IEnumerable<Poco> pocos)
        {
            if (pocos == null || !pocos.Any())
                throw new Exception("Cannot write DbContext: no POCOs created");

            var stubs = new List<Tuple<Poco, string>>();

            string indent = String.Empty;
            var builder = new StringBuilder();

            WriteHeader(builder, ref indent, pocos);
            builder.AppendLine(String.Format("{0}{{", indent));
            indent = indent.AddIndent();

            builder.AppendLine(String.Format("{0}public {1}(DbContextOptions<{2}> options)", indent, _settings.ClassName, _settings.ClassName));
            builder.AppendLine(String.Format("{0}\t: base(options)", indent));
            builder.AppendLine(String.Format("{0}{{", indent));
            builder.AppendLine(String.Format("{0}}}", indent));
            builder.AppendLine();

            //warnings
            if (!_settings.IncludeTablesWithoutPK)
            {
                foreach (var poco in FilterQueryTables(pocos))
                {
                    Log.Warning(String.Format("Table [{0}].[{1}] has no PK - DbContext will ignore it because setting 'IncludeTablesWithoutPK' is false",
                        poco.Table, poco.Table.Name));
                }
            }

            if (_settings.IncludeProperties)
            {
                foreach (var poco in FilterEntities(pocos))
                {
                    builder.AppendLine(String.Format("{0}public virtual DbSet<{1}> {2} {{ get; set; }}", indent, poco.Name, poco.Name));
                }
                builder.AppendLine();

                if (_settings.IncludeTablesWithoutPK)
                {
                    foreach (var poco in FilterQueryTables(pocos))
                    {
                        builder.AppendLine(String.Format("{0}public virtual DbQuery<{1}> {2} {{ get; set; }}", indent, poco.Name, poco.Name));
                    }
                    builder.AppendLine();
                }

                if (_settings.IncludeViews)
                {
                    foreach (var poco in FilterQueryViews(pocos))
                    {
                        builder.AppendLine(String.Format("{0}public virtual DbQuery<{1}> {2} {{ get; set; }}", indent, poco.Name, poco.Name));
                    }
                    builder.AppendLine();
                }
            }

            var modelBuilderName = "modelBuilder";
            if (_settings.IncludeOnModelCreating)
            {
                builder.AppendLine(String.Format("{0}protected override void OnModelCreating(ModelBuilder {1})", indent, modelBuilderName));
                builder.AppendLine(String.Format("{0}{{", indent));
                indent = indent.AddIndent();

                //foreach (var noPk in pocos.Where(p => !_sqlEngine.IsView(p.Table) && p.Table.Pk == null))
                //{
                //    Log.Warning(String.Format("Table [{0}].[{1}] has no PK - DbContext will ignore it", noPk.Table.Schema, noPk.Table.Name));
                //}

                foreach (var poco in FilterEntities(pocos))
                {
                    WriteEntity(builder, ref indent, modelBuilderName, stubs, poco);
                }

                if (_settings.IncludeTablesWithoutPK)
                {
                    foreach (var poco in FilterQueryTables(pocos))
                    {
                        WriteEntity(builder, ref indent, modelBuilderName, stubs, poco);
                    }
                }

                if (_settings.IncludeViews)
                {
                    foreach (var poco in FilterQueryViews(pocos))
                    {
                        WriteEntity(builder, ref indent, modelBuilderName, stubs, poco);
                    }
                }

                if (_settings.IncludeOnModelCreatingStubCall)
                {
                    builder.AppendLine(indent);
                    builder.AppendLine(String.Format("{0}OnModelCreatingNext({1});", indent, modelBuilderName));
                }

                indent = indent.RemoveIndent();
                builder.AppendLine(String.Format("{0}}}", indent));
            }
            
            if ((_settings.IncludeOptionalStubs || _settings.IncludeOnModelCreatingStubSignature) && _settings.SelfStub)
                WriteStubs(builder, ref indent, modelBuilderName, stubs);

            indent = indent.RemoveIndent();
            builder.AppendLine(String.Format("{0}}}", indent));
            indent = indent.RemoveIndent();
            builder.AppendLine(String.Format("{0}}}", indent));

            var directory = new FileInfo(_settings.OutputPath).Directory.FullName;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            WriteFile(builder, _settings.OutputPath, _settings.ClassName);
            
            if ((_settings.IncludeOptionalStubs || _settings.IncludeOnModelCreatingStubSignature) && !_settings.SelfStub)
            {
                var stubBuilder = new StringBuilder();
                indent = String.Empty;

                WriteHeader(stubBuilder, ref indent);
                stubBuilder.AppendLine(String.Format("{0}{{", indent));
                indent = indent.AddIndent();

                WriteStubs(stubBuilder, ref indent, modelBuilderName, stubs);

                indent = indent.RemoveIndent();
                stubBuilder.AppendLine(String.Format("{0}}}", indent));
                indent = indent.RemoveIndent();
                stubBuilder.AppendLine(String.Format("{0}}}", indent));

                directory = new FileInfo(_settings.StubOutputPath).Directory.FullName;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                WriteFile(stubBuilder, _settings.StubOutputPath, _settings.ClassName);
            }
        }

        private void WriteHeader(StringBuilder builder, ref string indent, IEnumerable<Poco> pocos = null)
        {
            //writing info
            if (!_globalSettings.ExcludeInfoText)
            {
                builder.AppendLine(InfoText.GeneratedCodeText);
                builder.AppendLine();
            }

            //usings
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Text;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using Microsoft.EntityFrameworkCore;");
            builder.AppendLine("using Microsoft.EntityFrameworkCore.Metadata;");
            builder.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
            if (pocos != null)
            {
                foreach (var pocoUsing in pocos.Select(p => p.Namespace)
                    .Where(n => !String.Equals(n, _settings.Namespace, StringComparison.InvariantCultureIgnoreCase)).Distinct())
                {
                    builder.AppendLine(String.Format("using {0};", pocoUsing));
                }
            }
            builder.AppendLine();
            builder.AppendLine(String.Format("namespace {0}", _settings.Namespace));
            builder.AppendLine("{");
            indent = indent.AddIndent();

            string qualifier = String.Concat(_settings.ClassAbstract ? "abstract " : String.Empty, "partial");
            if (_settings.Extends != null && _settings.Extends.Any())
                builder.AppendLine(String.Format("{0}public {1} class {2} : {3}", indent,
                    qualifier,
                    _settings.ClassName,
                    String.Join(", ", _settings.Extends)));
            else
                builder.AppendLine(String.Format("{0}public {1} class {2}", indent, qualifier, _settings.ClassName));
        }

        private void WriteEntity(StringBuilder builder, ref string indent, string modelBuilderName, List<Tuple<Poco, string>> stubs, Poco poco)
        {
            if (_sqlEngine.IsTable(poco.Table) && poco.Table.Pk != null)
                builder.AppendLine(String.Format("{0}{1}.Entity<{2}>(entity =>", indent, modelBuilderName, poco.Name));
            else if (_sqlEngine.IsView(poco.Table))
                builder.AppendLine(String.Format("{0}{1}.Query<{2}>(entity =>", indent, modelBuilderName, poco.Name));

            builder.AppendLine(String.Format("{0}{{", indent));
            indent = indent.AddIndent();

            //entity name
            if (_sqlEngine.IsView(poco.Table))
                builder.AppendLine(String.Format("{0}entity.ToView(\"{1}\", \"{2}\");", indent, poco.Table.Name, poco.Table.Schema));
            else if (_sqlEngine.IsTable(poco.Table))
            {
                if (poco.Table.Pk != null)
                {
                    builder.AppendLine(String.Format("{0}entity.ToTable(\"{1}\", \"{2}\");", indent, poco.Table.Name, poco.Table.Schema));
                }
                else
                {
                    //mandatory pre-stub
                    string stub = String.Concat(poco.Name, "Query");
                    stubs.Add(new Tuple<Poco, string>(poco, stub));
                    builder.AppendLine(String.Format("{0}{1}(entity);", indent, stub));
                }
            }

            //pk
            if (poco.Table.Pk != null && poco.Table.Pk.Columns.Any())
            {
                if (poco.Table.Pk.Columns.Count == 1)
                    builder.AppendLine(String.Format("{0}entity.HasKey(e => e.{1});", indent, 
                        poco.BaseProperties.First(p => p.Column == poco.Table.Pk.Columns.Values.First().Column).Name));
                else
                    builder.AppendLine(String.Format("{0}entity.HasKey(e => new {{ {1} }});",
                        indent,
                        String.Join(", ", poco.Table.Pk.Columns.Values.OrderBy(c => c.Position).Select(c => String.Concat("e.", poco.BaseProperties.First(p => p.Column == c.Column).Name)))));
            }

            //indices
            if (_settings.IncludeIndices && poco.Table.Pk != null && poco.Table.Indices != null && poco.Table.Indices.Any(idx => !idx.IsPrimaryKey))
            {
                foreach (var index in poco.Table.Indices.Where(idx => !idx.IsPrimaryKey))
                {
                    if (!index.Columns.Any())
                        continue;
                    if (index.Columns.Count() == 1)
                        builder.AppendLine(String.Format("{0}entity.HasIndex(e => e.{1});", indent, index.Columns.Values.First().Column.PocoProperty.Name));
                    else
                        builder.AppendLine(String.Format("{0}entity.HasIndex({1});",
                            indent,
                            String.Join(", ", index.Columns.Values.OrderBy(c => c.Position).Select(c => String.Concat("\"", c.Column.PocoProperty.Name, "\"")))));
                }
            }

            //columns
            foreach (var property in poco.BaseProperties)
            {
                var directives = new List<string>();

                if (property.Column.Dependencies != null && property.Column.Dependencies.Any())
                    directives.Add(".ValueGeneratedNever()");

                if (!String.Equals(property.Name, property.Column.Name, StringComparison.InvariantCulture))
                    directives.Add(String.Format(".HasColumnName(@\"{0}\")", property.Column.Name));

                if (property.Column.DataType.Equals("date", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("datetime", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("time", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("money", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("smallmoney", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("float", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("real", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (property.Column.DateTimePrecision != null)
                        directives.Add(String.Format(".HasColumnType(\"{0}({1})\")", property.Column.DataType, property.Column.DateTimePrecision));
                    else
                        directives.Add(String.Format(".HasColumnType(\"{0}\")", property.Column.DataType));
                }
                if (property.Column.DataType.Equals("decimal", StringComparison.InvariantCultureIgnoreCase)
                    || property.Column.DataType.Equals("numeric", StringComparison.InvariantCultureIgnoreCase))
                {
                    directives.Add(String.Format(".HasColumnType(\"{0}({1},{2})\")", 
                        property.Column.DataType, property.Column.NumericPrecision, property.Column.NumericScale));
                }

                if (_sqlEngine.IsString(property.Column))
                {
                    if (property.Column.CharacterMaximumLength >= 0)
                        directives.Add(String.Format(".HasMaxLength({0})", property.Column.CharacterMaximumLength));
                    directives.Add(String.Format(".IsUnicode({0})", _sqlEngine.IsUnicode(property.Column) ? "true" : "false"));
                }

                if (!property.Column.IsNullable && (poco.Table.Pk == null || !poco.Table.Pk.Columns.Values.Any(c => c.ColumnName.Equals(property.Column.Name))))
                    directives.Add(".IsRequired()");

                if (!String.IsNullOrWhiteSpace(property.Column.Default))
                    directives.Add(String.Format(".HasDefaultValueSql(\"{0}\")", property.Column.Default));

                if (property.Column.IsIdentity)
                {
                    var directive = _sqlEngine.GetIdentitySpecifier(property.Column);
                    if (!String.IsNullOrWhiteSpace(directive))
                        directives.Add(directive);
                }

                if (directives.Any())
                {
                    builder.AppendLine();
                    builder.Append(String.Format("{0}entity.Property(e => e.{1})", indent, property.Name));
                    if (directives.Count == 1)
                        builder.AppendLine(String.Concat(directives.First(), ";"));
                    else
                    {
                        indent = indent.AddIndent();
                        foreach (var directive in directives)
                        {
                            builder.AppendLine();
                            builder.Append(String.Format("{0}{1}", indent, directive));
                        }
                        builder.AppendLine(";");
                        indent = indent.RemoveIndent();
                    }
                }
            }

            //fk
            foreach (var property in poco.OutNavigationProperties)
            {
                var externalNavigationProperty = property.Poco.InNavigationProperties.FirstOrDefault(n => n.ForeignKey == property.ForeignKey);
                if (externalNavigationProperty != null)
                {

                    builder.AppendLine();
                    builder.AppendLine(String.Format("{0}entity.HasOne(f => f.{1})", indent, property.Name));
                    indent = indent.AddIndent();

                    if (property.ForeignKey.IsOne())
                        builder.AppendLine(String.Format("{0}.WithOne(p => p.{1})", indent, externalNavigationProperty.Name));
                    else
                        builder.AppendLine(String.Format("{0}.WithMany(p => p.{1})", indent, externalNavigationProperty.Name));

                    if (property.BaseProperties != null && property.BaseProperties.Any())
                    {
                        if (property.ForeignKey.IsOne())
                        {
                            builder.AppendLine(String.Format("{0}.HasForeignKey<{1}>({2})", indent,
                               poco.Name,
                               String.Join(", ", property.BaseProperties.Select(p => String.Concat("\"", p.Name, "\"")))));
                        }
                        else
                        {
                            builder.AppendLine(String.Format("{0}.HasForeignKey({1})", indent,
                               String.Join(", ", property.BaseProperties.Select(p => String.Concat("\"", p.Name, "\"")))));
                        }
                    }

                    var onDelete = _sqlEngine.ForeignKeyRuleString(property.ForeignKey.DeleteRuleStr);
                    if (onDelete != null)
                        builder.AppendLine(String.Format("{0}.OnDelete({1})", indent, onDelete));

                    builder.AppendLine(String.Format("{0}.HasConstraintName(\"{1}\");", indent, property.ForeignKey.Name));

                    indent = indent.RemoveIndent();
                }
            }

            //optional stub
            if (_settings.IncludeOptionalStubs)
            {
                string stub = String.Concat(poco.Name, "Init");
                stubs.Add(new Tuple<Poco, string>(poco, stub));
                builder.AppendLine(String.Format("{0}{1}(entity);", indent, stub));
            }

            indent = indent.RemoveIndent();
            builder.AppendLine(String.Format("{0}}});", indent));
            builder.AppendLine();
        }

        private void WriteStubs(StringBuilder builder, ref string indent, string modelBuilderName, IEnumerable<Tuple<Poco, string>> stubs)
        {
            //OnModelCreating stub
            if (_settings.IncludeOnModelCreatingStubSignature)
            {
                builder.AppendLine(indent);
                if (_settings.ClassAbstract)
                {
                    builder.AppendLine(String.Format("{0}protected abstract void OnModelCreatingNext(ModelBuilder {1});", indent, modelBuilderName));
                }
                else
                {
                    builder.AppendLine(String.Format("{0}protected virtual void OnModelCreatingNext(ModelBuilder {1})", indent, modelBuilderName));
                    builder.AppendLine(String.Format("{0}{{", indent));
                    builder.AppendLine(String.Format("{0}", indent));
                    builder.AppendLine(String.Format("{0}}}", indent));
                }
            }

            //entity stubs
            foreach (var stub in stubs)
            {
                builder.AppendLine(indent);
                if (_settings.ClassAbstract)
                {
                    builder.AppendLine(String.Format("{0}protected abstract void {1}(QueryTypeBuilder<{2}> entity);",
                        indent, stub.Item2, stub.Item1.Name));
                }
                else
                {
                    builder.AppendLine(String.Format("{0}protected virtual void {1}(QueryTypeBuilder<{2}> entity)",
                        indent, stub.Item2, stub.Item1.Name));
                    builder.AppendLine(String.Format("{0}{{", indent));
                    builder.AppendLine(String.Format("{0}", indent));
                    builder.AppendLine(String.Format("{0}}}", indent));
                }
            }
        }

        private IEnumerable<Poco> FilterEntities(IEnumerable<Poco> pocos)
        {
            return pocos.Where(e => _sqlEngine.IsTable(e.Table) && e.Table.Pk != null)
                .OrderBy(e => e.Name);
        }

        private IEnumerable<Poco> FilterQueryTables(IEnumerable<Poco> pocos)
        {
            return pocos.Where(e => _sqlEngine.IsTable(e.Table) && e.Table.Pk == null)
                .OrderBy(e => e.Name);
        }

        private IEnumerable<Poco> FilterQueryViews(IEnumerable<Poco> pocos)
        {
            return pocos.Where(e =>
                _sqlEngine.IsView(e.Table))
            .OrderBy(e => e.Name);
        }

        private void WriteFile(StringBuilder builder, string path, string className)
        {
            if (Directory.Exists(path))
                File.WriteAllText(Path.Combine(path, className + ".cs"), builder.ToString());
            else
                File.WriteAllText(path, builder.ToString());
        }
    }
}
