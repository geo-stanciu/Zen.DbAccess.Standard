using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.SqlServer.Standard.Extensions;
using Zen.DbAccess.Standard.Constants;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Extensions;
using Zen.DbAccess.Standard.Interfaces;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.SqlServer.Standard;

public class SqlServerDatabaseSpeciffic : DbSpeciffic
{
    public override DbConnection CreateConnection()
    {
        return new SqlConnection();
    }

    public override DbDataAdapter CreateDataAdapter(IZenDbConnection conn)
    {
        DbDataAdapter? da = new SqlDataAdapter();

        return da;
    }

    public override char EscapeCustomNameStartChar()
    {
        return '[';
    }

    public override char EscapeCustomNameEndChar()
    {
        return ']';
    }

    public override (string, SqlParam) PrepareEmptyParameter(DbModel model, PropertyInfo propertyInfo)
    {
        (string prmName, SqlParam prm) = ((IDbSpeciffic)this).CommonPrepareEmptyParameter(propertyInfo);

        if (!prm.isBlob && model.IsBlobDataType(propertyInfo))
        {
            prm.isBlob = true;
        }

        return (prmName, prm);
    }

    public override (string, SqlParam) PrepareParameter(DbModel model, PropertyInfo propertyInfo)
    {
        (string prmName, SqlParam prm) = ((IDbSpeciffic)this).CommonPrepareParameter(model, propertyInfo);

        if (!prm.isBlob && model.IsBlobDataType(propertyInfo))
        {
            prm.isBlob = true;
        }

        return (prmName, prm);
    }

    public override void EnsureTempTable(string table)
    {
        if (!table.StartsWith("#", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{table} must begin with #.");
        }
    }

    public override string GetGetServerDateTimeQuery()
    {
        string sql = "SELECT GETDATE()";

        return sql;
    }

    public override (string, IEnumerable<SqlParam>) GetInsertedIdQuery(string table, DbModel model, string firstPropertyName)
    {
        string sql = "; select SCOPE_IDENTITY() as ROW_ID;";

        return (sql, Array.Empty<SqlParam>());
    }

    public async Task BulkInsertAsync<T>(
        List<T> list,
        IZenDbConnection conn,
        string table,
        bool insertPrimaryKeyColumn = false) where T : DbModel
    {
        T? firstModel = list.FirstOrDefault();

        if (firstModel == null)
            throw new NullReferenceException(nameof(firstModel));

        firstModel.RefreshDbColumnsAndModelProperties(conn, table);

        var propertiesToInsert = firstModel.GetPropertiesToInsert(conn, insertPrimaryKeyColumn, table);

        using SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)conn.Connection, SqlBulkCopyOptions.Default, (SqlTransaction)conn.Transaction!);

        bulkCopy.DestinationTableName = table;

        bulkCopy.BatchSize = 5000;
        bulkCopy.BulkCopyTimeout = DbAccessConstants.DefaultCommandTimeoutSeconds;

        using DataTable dt = new DataTable();

        int k = 0;

        foreach (var property in propertiesToInsert)
        {
            string? dbColName = firstModel.GetMappedProperty(property.Name);

            Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (t.IsEnum || t.IsSubclassOf(typeof(Enum)))
            {
                dt.Columns.Add(dbColName, typeof(int));
            }
            else if (t == typeof(bool))
            {
                dt.Columns.Add(dbColName, typeof(int));
            }
            else
            {
                dt.Columns.Add(dbColName, t);
            }

            bulkCopy.ColumnMappings.Add(k++, dbColName!);
        }

        foreach (var item in list)
        {
            var values = new List<object>(propertiesToInsert.Count);

            foreach (var property in propertiesToInsert)
            {
                var val = property.GetValue(item);

                if (val == null)
                {
                    values.Add(DBNull.Value);
                    continue;
                }

                Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (t.IsEnum || t.IsSubclassOf(typeof(Enum)))
                {
                    values.Add((int)val);
                }
                else if (t == typeof(bool))
                {
                    values.Add((bool)val ? 1 : 0);
                }
                else
                {
                    values.Add(val);
                }
            }

            dt.Rows.Add(values.ToArray());
        }

        await Task.Run(() => bulkCopy.WriteToServer(dt))
            .ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    throw t.Exception;
                }
            }).ConfigureAwait(false);
    }
}
