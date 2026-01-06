using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.DatabaseSpeciffic;
using Zen.DbAccess.Enums;
using Zen.DbAccess.Extensions;
using Zen.DbAccess.Interfaces;
using Zen.DbAccess.Models;
using Zen.DbAccess.SqlServer.Extensions;
using Zen.DbAccess.Standard.DatabaseSpeciffic;

namespace Zen.DbAccess.SqlServer;

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
        if (!table.StartsWith("##", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{table} must begin with ##.");
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

    public override Tuple<string, SqlParam[]> PrepareBulkInsertBatchWithSequence<T>(
       List<T> list,
       IZenDbConnection conn,
       string table,
       bool insertPrimaryKeyColumn,
        string sequence2UseForPrimaryKey)
    {
        int k = -1;
        bool firstRow = true;
        StringBuilder sbInsert = new StringBuilder();
        List<SqlParam> insertParams = new List<SqlParam>();
        sbInsert.AppendLine($"insert into {table} ( ");

        T firstModel = list.First();
        firstModel.ResetDbModel();
        firstModel.RefreshDbColumnsAndModelProperties(conn, table);

        List<PropertyInfo> propertiesToInsert = firstModel.GetPropertiesToInsert(conn, insertPrimaryKeyColumn, table);

        for (int i = 0; i < list.Count; i++)
        {
            T model = list[i];

            k++;
            bool firstParam = true;
            StringBuilder sbInsertValues = new StringBuilder();

            foreach (PropertyInfo propertyInfo in propertiesToInsert)
            {
                if (firstParam)
                {
                    firstParam = false;
                }
                else
                {
                    if (firstRow)
                        sbInsert.Append(", ");

                    sbInsertValues.Append(", ");
                }

                string? dbCol = firstModel!.GetMappedProperty(propertyInfo.Name);

                if (!insertPrimaryKeyColumn
                    && !string.IsNullOrEmpty(dbCol)
                    && firstModel.IsPartOfThePrimaryKey(dbCol!))
                {
                    if (i == 0)
                        firstParam = true; // we don't add the primary key

                    continue;
                }

                if (firstRow)
                    sbInsert.Append($" {dbCol} ");

                sbInsertValues.Append($" @p_{propertyInfo.Name}_{k} ");

                SqlParam prm = new SqlParam($"@p_{propertyInfo.Name}_{k}", propertyInfo.GetValue(model));

                insertParams.Add(prm);
            }

            if (firstRow)
            {
                firstRow = false;
                sbInsert
                    .AppendLine(") values ")
                    .Append(" (")
                    .Append(sbInsertValues).AppendLine(")");
            }
            else
            {
                sbInsert.Append(", (").Append(sbInsertValues).AppendLine(")");
            }
        }

        return new Tuple<string, SqlParam[]>(sbInsert.ToString(), insertParams.ToArray());
    }

    public override Tuple<string, SqlParam[]> PrepareBulkInsertBatch<T>(
        List<T> list,
        IZenDbConnection conn,
        string table)
    {
        int k = -1;
        bool firstRow = true;
        StringBuilder sbInsert = new StringBuilder();
        List<SqlParam> insertParams = new List<SqlParam>();
        sbInsert.AppendLine($"insert into {table} ( ");

        T firstModel = list.First();
        firstModel.ResetDbModel();
        firstModel.RefreshDbColumnsAndModelProperties(conn, table);

        List<PropertyInfo> propertiesToInsert = firstModel.GetPropertiesToInsert(conn, insertPrimaryKeyColumn: false, table: table);

        for (int i = 0; i < list.Count; i++)
        {
            T model = list[i];

            k++;
            bool firstParam = true;
            StringBuilder sbInsertValues = new StringBuilder();

            foreach (PropertyInfo propertyInfo in propertiesToInsert)
            {
                if (firstParam)
                {
                    firstParam = false;
                }
                else
                {
                    if (firstRow)
                        sbInsert.Append(", ");

                    sbInsertValues.Append(", ");
                }

                string? dbCol = firstModel!.GetMappedProperty(propertyInfo.Name);

                if (firstRow)
                    sbInsert.Append($" {dbCol} ");

                sbInsertValues.Append($" @p_{propertyInfo.Name}_{k} ");

                SqlParam prm = new SqlParam($"@p_{propertyInfo.Name}_{k}", propertyInfo.GetValue(model));

                insertParams.Add(prm);
            }

            if (firstRow)
            {
                firstRow = false;
                sbInsert
                    .AppendLine(") values ")
                    .Append(" (")
                    .Append(sbInsertValues).AppendLine(")");
            }
            else
            {
                sbInsert.Append(", (").Append(sbInsertValues).AppendLine(")");
            }
        }

        return new Tuple<string, SqlParam[]>(sbInsert.ToString(), insertParams.ToArray());
    }
}
