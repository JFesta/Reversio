using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Entities
{
    public enum ForeignKeyRule
    {
        NoAction, Cascade, SetNull, SetDefault
    }
}
