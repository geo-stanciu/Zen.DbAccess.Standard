using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.SqlServer.Standard.Extensions;

public static class SqlServerDbModelExtensions
{
    public static bool IsBlobDataType(this DbModel dbModel, PropertyInfo propertyInfo)
    {
        Type t = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        return t == typeof(byte[]);
    }
}
