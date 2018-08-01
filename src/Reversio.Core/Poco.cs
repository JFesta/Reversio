using Reversio.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core
{
    public class Poco
    {
        public Table Table { get; set; }

        public string Name { get; set; }
        public string Namespace { get; set; }
        public IEnumerable<string> Usings { get; set; }
        public IEnumerable<string> Extends { get; set; }

        public string AccessModifier { get; set; }
        public bool IsPartial { get; set; }

        public List<PocoBaseProperty> BaseProperties { get; set; } = new List<PocoBaseProperty>();
        public List<PocoNavigationProperty> InNavigationProperties { get; set; } = new List<PocoNavigationProperty>();
        public List<PocoNavigationProperty> OutNavigationProperties { get; set; } = new List<PocoNavigationProperty>();
        public HashSet<string> PropertyNames { get; set; } = new HashSet<string>();
    }

    public abstract class PocoProperty
    {
        public string Name { get; set; }
    }

    public class PocoBaseProperty : PocoProperty
    {
        public Column Column { get; set; }
        public string CSharpType { get; set; }
    }

    public class PocoNavigationProperty : PocoProperty
    {
        public Poco Poco { get; set; }
        public ForeignKey ForeignKey { get; set; }
        public IEnumerable<PocoBaseProperty> BaseProperties { get; set; }
        public bool IsVirtual { get; set; }
    }
}
