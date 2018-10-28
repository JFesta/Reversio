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
using Reversio.Core.Settings;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reversio.Cli
{
    public class JsonSettings
    {
        public List<JsonJob> Jobs { get; set; }
    }

    public class JsonJob
    {
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public List<JsonStep> Steps { get; set; }
    }

    public class JsonStep
    {
        public string Name { get; set; }
        public string StepType { get; set; }
        public List<string> EntityTypes { get; set; }
        public List<string> Schemas { get; set; }
        public JsonDbFilterSetting Exclude { get; set; }
        
        public string Namespace { get; set; }
        public string ClassAccessModifier { get; set; }
        public bool ClassPartial { get; set; }
        public bool VirtualNavigationProperties { get; set; }
        public List<string> Usings { get; set; }
        public List<string> Extends { get; set; }
        public bool ClassNameForcePascalCase { get; set; }
        public List<JsonNameReplaceRegex> ClassNameReplace { get; set; }
        public bool PropertyNameForcePascalCase { get; set; }
        public List<JsonNameReplaceRegex> PropertyNameReplace { get; set; }
        public bool PropertyNullableIfDefaultAndNotPk { get; set; }

        public string OutputPath { get; set; }
        public string StubOutputPath { get; set; }
        public bool CleanFolder { get; set; }

        public string ClassName { get; set; }
        public bool ClassAbstract { get; set; }
        public bool IncludeProperties { get; set; } = true;
        public bool IncludeOnModelCreating { get; set; } = true;
        public string IncludeOnModelCreatingStub { get; set; }
        public bool IncludeOptionalStubs { get; set; }
        public bool IncludeIndices { get; set; }
        public bool IncludeViews { get; set; }
        public bool IncludeTablesWithoutPK { get; set; }
    }

    public class JsonDbFilterSetting
    {
        public List<string> ExcludeExact { get; set; }
        public List<string> ExcludeRegex { get; set; }
        

        public DbFilterSetting Convert()
        {
            return new DbFilterSetting()
            {
                MatchExact = (ExcludeExact != null)
                    ? ExcludeExact.Select(s => Split(s)).ToList()
                    : null,
                MatchRegexes = ExcludeRegex
                    .Where(s => !String.IsNullOrWhiteSpace(s))
                    .Select(s => new Regex(s, RegexOptions.IgnoreCase))
            };
        }

        private Tuple<string, string> Split(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return new Tuple<string, string>(null, value);

            var split = value.Split('.');
            return (split.Length < 2)
                ? new Tuple<string, string>(null, value)
                : new Tuple<string, string>(split[0], String.Concat(split.Skip(1)));
        }
    }

    public class JsonNameReplaceRegex
    {
        public string Regex { get; set; }
        public string ReplaceWith { get; set; }

        public NameReplaceRegex Convert()
        {
            return new NameReplaceRegex()
            {
                Regex = new System.Text.RegularExpressions.Regex(Regex),
                ReplaceWith = ReplaceWith
            };
        }
    }
}
