using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zen.DbAccess.Standard.Attributes;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.Postgresql.Standard.Extensions;

public static class PostgresqlDbModelExtentions
{
    public static bool IsJsonDataType(this DbModel dbModel, PropertyInfo propertyInfo)
    {
        return Attribute.IsDefined(propertyInfo, typeof(JsonDbTypeAttribute));
    }
}
