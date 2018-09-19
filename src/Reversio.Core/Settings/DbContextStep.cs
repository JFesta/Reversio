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

namespace Reversio.Core.Settings
{
    public class DbContextStep : IStep
    {
        public string OutputPath { get; set; }
        public string StubOutputPath { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public bool ClassAbstract { get; set; }
        public IEnumerable<string> Extends { get; set; }
        public bool IncludeIndices { get; set; }
        public string IncludeOnModelCreatingStub { get; set; }
        public bool IncludeViews { get; set; }
        public bool IncludeTablesWithoutPK { get; set; }

        public bool IncludeStubs
        {
            get
            {
                return !String.IsNullOrWhiteSpace(StubOutputPath);
            }
        }

        public bool SelfStub
        {
            get
            {
                return String.IsNullOrWhiteSpace(StubOutputPath)
                    || String.Equals(OutputPath, StubOutputPath, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public bool IncludeOnModelCreatingStubCall
        {
            get
            {
                return
                    String.Equals(IncludeOnModelCreatingStub, "Call", StringComparison.InvariantCultureIgnoreCase)
                    || String.Equals(IncludeOnModelCreatingStub, "Signature", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public bool IncludeOnModelCreatingStubSignature
        {
            get
            {
                return String.Equals(IncludeOnModelCreatingStub, "Signature", StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}
