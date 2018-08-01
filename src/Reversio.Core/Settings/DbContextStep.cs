using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Settings
{
    public class DbContextStep : IStep
    {
        public string OutputPath { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<string> Extends { get; set; }
        public bool IncludeIndices { get; set; }
    }
}
