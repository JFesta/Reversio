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
using System.Text;

namespace Reversio.Core.Settings
{
    public class PocoGenerateStep : IStep
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string ClassAccessModifier { get; set; }
        public bool ClassPartial { get; set; }
        public bool VirtualNavigationProperties { get; set; }
        public IEnumerable<string> Usings { get; set; }
        public IEnumerable<string> Extends { get; set; }
        public bool ClassNameForcePascalCase { get; set; }
        public IEnumerable<NameReplaceRegex> ClassNameReplace { get; set; }
        public bool PropertyNameForcePascalCase { get; set; }
        public IEnumerable<NameReplaceRegex> PropertyNameReplace { get; set; }
        public bool PropertyNullableIfDefaultAndNotPk { get; set; }
    }
}
