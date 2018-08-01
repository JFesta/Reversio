using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Settings
{
    public class PocoWriteStep : IStep
    {
        public string OutputPath { get; set; }
        public bool CleanFolder { get; set; }
        public DbFilterSetting PocosExclude { get; set; }
    }
}
