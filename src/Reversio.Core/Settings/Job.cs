using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Settings
{
    public class Job
    {
        public string SettingsFile { get; set; }
        public string WorkingDirectory { get; set; }
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public List<IStep> Steps { get; set; } = new List<IStep>();
    }
}
