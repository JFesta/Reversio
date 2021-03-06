﻿/*
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
