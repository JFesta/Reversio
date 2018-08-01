using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Settings
{
    public class LoadStep : IStep
    {
        public IEnumerable<string> EntityTypes { get; set; }
        public IEnumerable<string> Schemas { get; set; }
        public DbFilterSetting Exclude { get; set; }
    }
}
