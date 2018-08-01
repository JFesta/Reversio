using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Settings
{
    public class PocoGenerateStep : IStep
    {
        public string Namespace { get; set; }
        public string ClassAccessModifier { get; set; }
        public bool ClassPartial { get; set; }
        public bool VirtualNavigationProperties { get; set; }
        public IEnumerable<string> Usings { get; set; }
        public IEnumerable<string> Extends { get; set; }
        public bool ClassNameForceCamelCase { get; set; }
        public IEnumerable<NameReplaceRegex> ClassNameReplace { get; set; }
        public bool PropertyNameForceCamelCase { get; set; }
        public IEnumerable<NameReplaceRegex> PropertyNameReplace { get; set; }
        public bool PropertyNullableIfDefaultAndNotPk { get; set; }
    }
}
