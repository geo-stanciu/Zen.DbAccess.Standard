using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zen.DbAccess.Standard.Attributes;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.Oracle.Standard.Extensions;

public static class OracleDbModelExtensions
{
    public static bool IsClobDataType(this DbModel dbModel, PropertyInfo propertyInfo)
    {
        return Attribute.IsDefined(propertyInfo, typeof(ClobDbTypeAttribute))
            || Attribute.IsDefined(propertyInfo, typeof(JsonDbTypeAttribute));
    }

    public static bool IsBlobDataType(this DbModel dbModel, PropertyInfo propertyInfo)
    {
        Type t = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        return t == typeof(byte[]);
    }
}
