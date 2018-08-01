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
