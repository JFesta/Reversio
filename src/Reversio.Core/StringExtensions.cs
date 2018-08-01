using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core
{
    public static class StringExtensions
    {
        public static string AddIndent(this string indent)
        {
            return indent + "\t";
        }

        public static string RemoveIndent(this string indent)
        {
            return String.IsNullOrEmpty(indent)
                ? String.Empty
                : indent.Substring(1);
        }
    }
}
