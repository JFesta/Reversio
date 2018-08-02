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
using Reversio.Core.Logging;
using Reversio.Core.Settings;
using Reversio.Core.SqlEngine;
using Reversio.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reversio.Core
{
    public class PocoEngine
    {
        private ISqlEngine _sqlEngine;

        public PocoEngine(ISqlEngine sqlEngine)
        {
            _sqlEngine = sqlEngine;
        }

        public IEnumerable<Poco> GeneratePocos(IEnumerable<Table> entities, PocoGenerateStep settings)
        {
            if (settings == null)
                throw new Exception("Cannot generate POCOs: no settings found");
            if (entities == null || !entities.Any())
                throw new Exception("Cannot generate POCOs: no entities loaded");

            var pocos = new List<Poco>();

            //create POCOs and resolve name conflicts
            foreach (var entity in entities.OrderBy(e => e.Name))
            {
                var poco = new Poco()
                {
                    Table = entity,
                    Name = entity.Name,
                    Namespace = settings.Namespace,
                    Usings = settings.Usings,
                    Extends = settings.Extends,
                    AccessModifier = settings.ClassAccessModifier,
                    IsPartial = settings.ClassPartial,
                };
                
                //name replace & resolve
                poco.Name = Replace(settings.ClassNameReplace, poco.Name);
                if (settings.ClassNameForcePascalCase)
                    poco.Name = ToPascalCase(poco.Name);
                ResolvePocoName(pocos, poco);

                Log.Debug(String.Format("POCO {0} - Assigned name: {1}", entity.Name, poco.Name));

                //properties: creation, name replace & resolve
                foreach (var column in entity.Columns.Values.OrderBy(c => c.Position))
                {
                    var property = new PocoBaseProperty()
                    {
                        Name = column.Name,
                        Column = column,
                        CSharpType = _sqlEngine.GetCSharpType(column, settings.PropertyNullableIfDefaultAndNotPk)
                    };
                    column.PocoProperty = property;

                    property.Name = Replace(settings.PropertyNameReplace, property.Name);
                    if (settings.PropertyNameForcePascalCase)
                        property.Name = ToPascalCase(property.Name);
                    property.Name = ResolvePropertyName(poco, property, "Property");

                    poco.BaseProperties.Add(property);
                    poco.PropertyNames.Add(property.Name);
                }
                pocos.Add(poco);
                entity.Poco = poco;
            }

            //create navigation properties
            foreach (var entity in entities)
            {
                //output dependencies
                if (entity.Dependencies != null)
                {
                    foreach (var fk in entity.Dependencies.Where(fk => fk.PkTable.Poco != null))
                    {
                        var property = new PocoNavigationProperty()
                        {
                            Name = AssignNavigationPropertyName(settings, entity.Poco, fk),
                            IsVirtual = settings.VirtualNavigationProperties,
                            Poco = fk.PkTable.Poco,
                            ForeignKey = fk,
                            BaseProperties = fk.Columns.Values.Select(c => c.FkColumn.PocoProperty)
                        };
                        property.Name = ResolvePropertyName(entity.Poco, property, "Navigation");
                        entity.Poco.OutNavigationProperties.Add(property);
                        entity.Poco.PropertyNames.Add(property.Name);
                    }
                }
                //input dependencies
                if (entity.DependenciesFrom != null)
                {
                    foreach (var fk in entity.DependenciesFrom.Where(fk => fk.FkTable.Poco != null))
                    {
                        var property = new PocoNavigationProperty()
                        {
                            Name = AssignNavigationPropertyName(settings, entity.Poco, fk),
                            IsVirtual = settings.VirtualNavigationProperties,
                            Poco = fk.FkTable.Poco,
                            ForeignKey = fk
                        };

                        property.Name = ResolvePropertyName(entity.Poco, property, "Navigation");
                        entity.Poco.InNavigationProperties.Add(property);
                        entity.Poco.PropertyNames.Add(property.Name);
                    }
                }
            }
            return pocos;
        }

        public void WritePocos(IEnumerable<Poco> pocos, PocoWriteStep settings)
        {
            if (settings == null)
                throw new Exception("Cannot write POCOs: no settings found");
            if (pocos == null || !pocos.Any())
                throw new Exception("Cannot write POCOs: no POCOs created");

            if (settings.CleanFolder)
                CleanDirectoryContent(settings.OutputPath);

            foreach (var poco in pocos
                .Where(p => settings.PocosExclude == null || !settings.PocosExclude.IsMatch(p.Table.Schema, p.Table.Name)))
            {
                string indent = String.Empty;
                var builder = new StringBuilder();

                //writing info
                builder.AppendLine(InfoText.GeneratedCodeText);
                builder.AppendLine();

                builder.AppendLine("using System;");
                builder.AppendLine("using System.Text;");
                builder.AppendLine("using System.Collections.Generic;");
                if (poco.Usings != null)
                {
                    foreach (var reference in poco.Usings)
                    {
                        builder.AppendLine(String.Format("using {0};", reference));
                    }
                }
                builder.AppendLine();
                builder.AppendLine(String.Format("namespace {0}", poco.Namespace));
                builder.AppendLine("{");
                indent = indent.AddIndent();
                if (poco.Extends != null && poco.Extends.Any())
                    builder.AppendLine(String.Format("{0}{1}{2} class {3} : {4}", indent, 
                        poco.AccessModifier, 
                        poco.IsPartial ? " partial" : String.Empty,
                        poco.Name,
                        String.Join(", ", poco.Extends)));
                else
                    builder.AppendLine(String.Format("{0}{1}{2} class {3}", indent, 
                        poco.AccessModifier,
                        poco.IsPartial ? " partial" : String.Empty,
                        poco.Name));
                builder.AppendLine(String.Format("{0}{{", indent));

                indent = indent.AddIndent();

                //constructor
                if (poco.InNavigationProperties.Any(p => !p.ForeignKey.IsOne()))
                {
                    builder.AppendLine(String.Format("{0}public {1}()", indent, poco.Name));
                    builder.AppendLine(String.Format("{0}{{", indent));
                    indent = indent.AddIndent();
                    foreach (var navigation in poco.InNavigationProperties.Where(p => !p.ForeignKey.IsOne()))
                    {
                        builder.AppendLine(String.Format("{0}{1} = new HashSet<{2}>();", indent, navigation.Name /*+ "Navigation"*/, navigation.Poco.Name));
                    }
                    indent = indent.RemoveIndent();
                    builder.AppendLine(String.Format("{0}}}", indent));
                    builder.AppendLine();
                }

                //properties
                foreach (var property in poco.BaseProperties)
                {
                    builder.AppendLine(String.Format("{0}public {1} {2} {{ get; set; }}", indent, property.CSharpType, property.Name));
                }

                //navigation properties
                if (poco.InNavigationProperties.Any())
                {
                    builder.AppendLine();
                    foreach (var property in poco.InNavigationProperties)
                    {
                        if (property.ForeignKey.IsOne())
                            builder.AppendLine(String.Format("{0}public {1}{2} {3} {{ get; set; }}", indent, 
                                property.IsVirtual ? "virtual " : String.Empty,
                                property.Poco.Name, property.Name));
                        else
                            builder.AppendLine(String.Format("{0}public ICollection<{1}> {2} {{ get; set; }}", indent, property.Poco.Name, property.Name));
                    }
                }
                if (poco.OutNavigationProperties.Any())
                {
                    builder.AppendLine();
                    foreach (var property in poco.OutNavigationProperties)
                    {
                        builder.AppendLine(String.Format("{0}public {1}{2} {3} {{ get; set; }}", indent,
                            property.IsVirtual ? "virtual " : String.Empty,
                            property.Poco.Name, property.Name));
                    }
                }

                indent = indent.RemoveIndent();
                builder.AppendLine(String.Format("{0}}}", indent));
                builder.AppendLine("}");
                File.WriteAllText(Path.Combine(settings.OutputPath, poco.Name + ".cs"), builder.ToString());
            }
        }

        public void ResolvePocoName(IEnumerable<Poco> allPocos, Poco poco)
        {
            int i = 0;
            string name = poco.Name;
            while (allPocos.Any(p => p != poco 
                && p.Name.Equals(name, StringComparison.InvariantCulture)
                && p.Namespace.Equals(poco.Namespace, StringComparison.InvariantCulture)))
            {
                i++;
                name = String.Concat(poco.Name, i.ToString());
            }
            poco.Name = name;
        }

        private string AssignNavigationPropertyName(PocoGenerateStep settings, Poco poco, ForeignKey fk)
        {
            string name;
            if ((poco == fk.FkTable.Poco && fk.FkTable.Dependencies != null && fk.FkTable.Dependencies.Count(f => f.PkTable == fk.PkTable) > 1)
                || (poco == fk.PkTable.Poco && fk.PkTable.DependenciesFrom != null && fk.PkTable.DependenciesFrom.Count(f => f.FkTable == fk.FkTable) > 1))
            {
                name = fk.Name;
            }
            else if (poco == fk.FkTable.Poco)
            {
                name = fk.PkTable.Poco.Name;
            }
            else
            {
                name = fk.FkTable.Poco.Name;
            }

            if (settings.PropertyNameForcePascalCase)
                name = ToPascalCase(name);
            if (name.StartsWith("fk_", StringComparison.InvariantCultureIgnoreCase))
                name = name.Substring(3);
            name = name.Replace("_", String.Empty);
            
            if (poco == fk.FkTable.Poco && name.StartsWith(poco.Table.Name, StringComparison.InvariantCultureIgnoreCase))
                name = name.Substring(poco.Table.Name.Length);
            return name;
        }

        private string ResolvePropertyName(Poco poco, PocoProperty property, string suffix)
        {
            string name = property.Name;
            if (poco.Name.Equals(property.Name))
                name = name + suffix;
            if (!poco.PropertyNames.Contains(name))
                return name;

            int i = 0;
            if (!name.EndsWith(suffix))
                name = property.Name + suffix;
            var baseName = name;
            while (poco.PropertyNames.Contains(name))
            {
                i++;
                name = String.Concat(baseName, i.ToString());
            }
            return name;
        }

        private string ToPascalCase(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return input;

            var array = input.ToCharArray();
            int start = -1;

            if (Char.IsLower(array[0]))
                array[0] = Char.ToUpperInvariant(array[0]);
            for (int i = 0; i < array.Length; i++)
            {
                if (Char.IsUpper(array[i]))
                {
                    if (start < 0)
                        start = i;
                }
                else if (start >= 0)
                {
                    int end = Char.IsLower(input[i]) ? i - 2 : i - 1;
                    int length = end - start + 1;
                    if (end > start && length >= 2)
                    {
                        for (int j = start + 1; j <= end; j++)
                        {
                            array[j] = Char.ToLowerInvariant(array[j]);
                        }
                    }
                    start = -1;
                }
            }
            if (start >= 0 && (array.Length - start) >= 2)
            {
                for (int j = start + 1; j < array.Length; j++)
                {
                    array[j] = Char.ToLowerInvariant(array[j]);
                }
            }
            return new string(array);
        }

        private string Replace(IEnumerable<NameReplaceRegex> regexes, string input)
        {
            if (regexes == null)
                return input;

            foreach (var regex in regexes)
            {
                input = regex.Regex.Replace(input, regex.ReplaceWith);
            }
            return input;
        }

        private void CleanDirectoryContent(string path)
        {
            var dir = new DirectoryInfo(path);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                d.Delete(true);
            }
        }
    }
}
