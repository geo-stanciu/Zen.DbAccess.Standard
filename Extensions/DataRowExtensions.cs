using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Standard.Attributes;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Helpers;
using Zen.DbAccess.Standard.Models;
using Zen.DbAccess.Standard.Utils;

namespace Zen.DbAccess.Standard.Extensions;

public static class DataRowExtensions
{
    public static T ToModel<T>(this DataRow row, ref Dictionary<string, PropertyInfo>? properties, ref bool propertiesAlreadyDetermined)
    {
        Type classType = typeof(T);
        T? rez = (T?)Activator.CreateInstance(classType);

        if (row == null)
            throw new NullReferenceException(nameof(rez));

        if (properties == null)
            properties = new Dictionary<string, PropertyInfo>();

        var customColumnNames = classType
            .GetProperties()
            .Select(x => new { x.Name, Attributes = x.GetCustomAttributes<DbNameAttribute>()?.ToList() })
            .Where(x => x.Attributes != null && x.Attributes.Count > 0)
            .ToDictionary(x => x.Name, y => y.Attributes!);

        for (int i = 0; i < row.Table.Columns.Count; i++)
        {
            string colName = row.Table.Columns[i].ColumnName;
            PropertyInfo? p = null;

            if (propertiesAlreadyDetermined)
            {
                if (!properties.TryGetValue(colName, out p))
                    continue;
            }
            else
            {
                p = ColumnNameMapUtils.GetModelPropertyForDbColumn(classType, colName, customColumnNames);

                if (p == null)
                    continue;

                properties[colName] = p;
            }

            object val = row[i];

            PropertyMapHelper.SetPropertyValue<T>(rez, p, val);
        }

        return rez!;
    }
}
