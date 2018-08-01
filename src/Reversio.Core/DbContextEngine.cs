using Reversio.Core.Logging;
using Reversio.Core.Settings;
using Reversio.Core.SqlEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reversio.Core
{
    public class DbContextEngine
    {
        private DbContextStep _settings;
        private ISqlEngine _sqlEngine;

        public DbContextEngine(DbContextStep settings, ISqlEngine sqlEngine)
        {
            _settings = settings;
            _sqlEngine = sqlEngine;
        }

        public void WriteDbContext(IEnumerable<Poco> pocos)
        {
            if (pocos == null || !pocos.Any())
                throw new Exception("Cannot write DbContext: no POCOs created");

            string indent = String.Empty;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Text;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using Microsoft.EntityFrameworkCore;");
            builder.AppendLine("using Microsoft.EntityFrameworkCore.Metadata;");
            foreach (var pocoUsing in pocos.Select(p => p.Namespace).Where(n => !String.Equals(n, _settings.Namespace)).Distinct())
            {
                builder.AppendLine(String.Format("using {0};", pocoUsing));
            }
            builder.AppendLine();
            builder.AppendLine(String.Format("namespace {0}", _settings.Namespace));
            builder.AppendLine("{");
            indent = indent.AddIndent();
            if (_settings.Extends != null && _settings.Extends.Any())
                builder.AppendLine(String.Format("{0}public partial class {1} : {2}", indent,
                    _settings.ClassName,
                    String.Join(", ", _settings.Extends)));
            else
                builder.AppendLine(String.Format("{0}public partial class {1}", indent, _settings.ClassName));
            builder.AppendLine(String.Format("{0}{{", indent));
            indent = indent.AddIndent();

            builder.AppendLine(String.Format("{0}public {1}(DbContextOptions<{2}> options)", indent, _settings.ClassName, _settings.ClassName));
            builder.AppendLine(String.Format("{0}\t: base(options)", indent));
            builder.AppendLine(String.Format("{0}{{", indent));
            builder.AppendLine(String.Format("{0}}}", indent));
            builder.AppendLine();

            foreach (var poco in Filter(pocos))
            {
                builder.AppendLine(String.Format("{0}public virtual DbSet<{1}> {2} {{ get; set; }}", indent, poco.Name, poco.Name));
            }
            builder.AppendLine();

            var modelBuilderName = "modelBuilder";
            builder.AppendLine(String.Format("{0}protected override void OnModelCreating(ModelBuilder {1})", indent, modelBuilderName));
            builder.AppendLine(String.Format("{0}{{", indent));
            indent = indent.AddIndent();

            foreach (var noPk in pocos.Where(p => !_sqlEngine.IsView(p.Table) && p.Table.Pk == null))
            {
                Log.Warning(String.Format("Table [{0}].[{1}] has no PK - DbContext will ignore it", noPk.Table.Schema, noPk.Table.Name));
            }

            foreach (var poco in Filter(pocos))
            {
                builder.AppendLine(String.Format("{0}{1}.Entity<{2}>(entity =>", indent, modelBuilderName, poco.Name));
                builder.AppendLine(String.Format("{0}{{", indent));
                indent = indent.AddIndent();

                //entity name
                //if (!String.Equals(poco.Name, poco.Table.Name, StringComparison.InvariantCulture))
                builder.AppendLine(String.Format("{0}entity.ToTable(\"{1}\", \"{2}\");", indent, poco.Table.Name, poco.Table.Schema));

                //pk
                if (poco.Table.Pk != null && poco.Table.Pk.Columns.Any())
                {
                    if (poco.Table.Pk.Columns.Count == 1)
                        builder.AppendLine(String.Format("{0}entity.HasKey(e => e.{1});", indent, poco.Table.Pk.Columns.Values.First().ColumnName));
                    else
                        builder.AppendLine(String.Format("{0}entity.HasKey(e => new {{ {1} }});",
                            indent,
                            String.Join(", ", poco.Table.Pk.Columns.Values.OrderBy(c => c.Position).Select(c => String.Concat("e.", c.ColumnName)))));
                }

                //indices
                if (_settings.IncludeIndices && poco.Table.Indices != null && poco.Table.Indices.Any(idx => !idx.IsPrimaryKey))
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
                        || property.Column.DataType.Equals("money", StringComparison.InvariantCultureIgnoreCase)
                        || property.Column.DataType.Equals("time", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (property.Column.DateTimePrecision != null)
                            directives.Add(String.Format(".HasColumnType(\"{0}({1})\")", property.Column.DataType, property.Column.DateTimePrecision));
                        else
                            directives.Add(String.Format(".HasColumnType(\"{0}\")", property.Column.DataType));
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
                    builder.AppendLine();
                    builder.AppendLine(String.Format("{0}entity.HasOne(f => f.{1})", indent, property.Name));
                    indent = indent.AddIndent();

                    var externalNavigationProperty = property.Poco.InNavigationProperties.FirstOrDefault(n => n.ForeignKey == property.ForeignKey);
                    if (externalNavigationProperty != null)
                    {
                        if (property.ForeignKey.IsOne())
                            builder.AppendLine(String.Format("{0}.WithOne(p => p.{1})", indent, externalNavigationProperty.Name));
                        else
                            builder.AppendLine(String.Format("{0}.WithMany(p => p.{1})", indent, externalNavigationProperty.Name));
                    }

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

                indent = indent.RemoveIndent();
                builder.AppendLine(String.Format("{0}}});", indent));
                builder.AppendLine();
            }

            indent = indent.RemoveIndent();
            builder.AppendLine(String.Format("{0}}}", indent));
            indent = indent.RemoveIndent();
            builder.AppendLine(String.Format("{0}}}", indent));
            indent = indent.RemoveIndent();
            builder.AppendLine(String.Format("{0}}}", indent));

            if (Directory.Exists(_settings.OutputPath))
                File.WriteAllText(Path.Combine(_settings.OutputPath, _settings.ClassName + ".cs"), builder.ToString());
            else
                File.WriteAllText(_settings.OutputPath, builder.ToString());
        }

        private IEnumerable<Poco> Filter(IEnumerable<Poco> pocos)
        {
            return pocos.Where(e => 
                !_sqlEngine.IsView(e.Table) && e.Table.Pk != null)
            .OrderBy(e => e.Name);
        }
    }
}
