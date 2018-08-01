using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reversio.Core.Settings
{
    public class DbFilterSetting
    {
        public IEnumerable<Tuple<string, string>> MatchExact { get; set; }
        public IEnumerable<Regex> MatchRegexes { get; set; }

        public bool IsMatch(string schema, string name)
        {
            if (MatchExact != null && MatchExact.Any())
            {
                if (String.IsNullOrWhiteSpace(schema))
                {
                    if (MatchExact.Any(m => m.Item2.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                        return true;
                }
                else
                {
                    if (MatchExact.Any(m =>
                        String.IsNullOrWhiteSpace(m.Item1)
                            ? m.Item2.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                            : m.Item1.Equals(schema, StringComparison.InvariantCultureIgnoreCase) 
                                && m.Item2.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                        return true;
                }
            }

            if (MatchRegexes != null && MatchRegexes.Any())
            {
                if (MatchRegexes.Any(r => r.IsMatch(name)))
                    return true;
            }
            return false;
        }
    }
}
