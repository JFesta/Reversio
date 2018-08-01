using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reversio.Core.Utils
{
    public static class PathUtils
    {
        private static string[] _separators = new[] { "\\", "/" };

        public static bool IsAbsolutePath(string path)
        {
            return Path.IsPathRooted(path)
                && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        public static bool IsDirectory(string path)
        {
            if (Directory.Exists(path))
                return true;

            if (File.Exists(path))
                return false;
            
            if (_separators.Any(x => path.EndsWith(x)))
                return true;
            
            return String.IsNullOrWhiteSpace(Path.GetExtension(path));
        }
    }
}
