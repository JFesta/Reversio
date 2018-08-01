using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Reversio.Core.Settings
{
    public class NameReplaceRegex
    {
        public Regex Regex { get; set; }
        public string ReplaceWith { get; set; }
    }
}
