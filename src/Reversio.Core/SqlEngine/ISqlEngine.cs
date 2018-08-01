using Reversio.Core.Entities;
using Reversio.Core.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.SqlEngine
{
    public interface ISqlEngine
    {
        IEnumerable<Table> Load(LoadStep settings);

        string GetCSharpType(Column column, bool nullableIfDefaultAndNotPk);
        bool IsView(Table entity);
        bool IsString(Column column);
        bool IsUnicode(Column column);
        string ForeignKeyRuleString(string rule);
    }
}
