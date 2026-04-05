using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Sqlite.Standard.Constants;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Extensions;
using Zen.DbAccess.Standard.Interfaces;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.Sqlite.Standard;

public class SqliteDatabaseSpeciffic : DbSpeciffic
{
    public override DbConnection CreateConnection()
    {
        return new SqliteConnection();
    }

    public DbProviderFactory BuildDbProviderFactory(DbConnectionType dbType)
    {
        var factory = SqliteFactory.Instance;
        return factory;
    }

    public override (string, SqlParam) PrepareParameter(DbModel model, PropertyInfo propertyInfo)
    {
        (string prmName, SqlParam prm) = ((IDbSpeciffic)this).CommonPrepareParameter(model, propertyInfo);

        return (prmName, prm);
    }

    public override object GetValueForPreparedParameter(DbModel dbModel, PropertyInfo propertyInfo)
    {
        var val = propertyInfo.GetValue(dbModel) ?? DBNull.Value;

        return val!;
    }

    public override void EnsureTempTable(string table)
    {
        if (!table.StartsWith("temp_", StringComparison.OrdinalIgnoreCase)
            && !table.StartsWith("tmp_", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{table} must begin with temp_ or tmp_ .");
        }
    }

    public override string GetGetServerDateTimeQuery()
    {
        string sql = "SELECT current_timestamp";

        return sql;
    }

    public override (string, IEnumerable<SqlParam>) GetInsertedIdQuery(string table, DbModel model, string firstPropertyName)
    {
        string sql = "; select last_insert_rowid() as ROW_ID;";

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

        var sqlParamNames = new Dictionary<string, string>();

        var sbSql = new StringBuilder();

        sbSql.Append($"INSERT INTO {table} (");

        bool isFirst = true;

        foreach (var property in propertiesToInsert)
        {
            string? dbCol = firstModel.GetMappedProperty(property.Name);

            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                sbSql.Append(", ");
            }

            sbSql.Append(dbCol);

            var prmName = $"@p_{property.Name}";

            sqlParamNames[property.Name] = prmName;
        }

        sbSql.Append(") VALUES ");

        int offset = 0;
        int dbMaxBatchSize = (int)Math.Floor((decimal)SqliteConstants.MaxParametersPerQuery / propertiesToInsert.Count);

        if (dbMaxBatchSize == 0)
        {
            throw new InvalidOperationException("The number of properties to insert exceeds the maximum allowed parameters per query.");
        }

        int batchSize = Math.Min(1024, dbMaxBatchSize);

        while (offset < list.Count)
        {
            var sbValues = new StringBuilder();

            var items = list.Skip(offset).Take(batchSize).ToList();
            offset += items.Count;

            var sqlParams = new SqlParam[items.Count * propertiesToInsert.Count];

            int k = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    sbValues.Append(", ");
                }

                sbValues.Append("(");

                var item = items[i];

                isFirst = true;

                foreach (var property in propertiesToInsert)
                {
                    string prmName = $"{sqlParamNames[property.Name]}_{i}";

                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sbValues.Append(", ");
                    }

                    sbValues.Append(prmName);

                    var val = property.GetValue(item);

                    sqlParams[k++] = new SqlParam(prmName, val);
                }

                sbValues.Append(")");
            }

            string sql = $"{sbSql} {sbValues}";

            _ = await sql.ExecuteNonQueryAsync(conn, sqlParams);
        }
    }
}
