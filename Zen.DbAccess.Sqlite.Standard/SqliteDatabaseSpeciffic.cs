using Microsoft.Data.Sqlite;
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
using Zen.DbAccess.Standard.DatabaseSpeciffic;

namespace Zen.DbAccess.Sqlite;

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

    public override DbParameter CreateDbParameter(DbCommand cmd, SqlParam prm)
    {
        DbParameter param = cmd.CreateParameter();

        string baseParameterName = prm.name.StartsWith("@") ? prm.name.Substring(1) : prm.name;
        param.ParameterName = baseParameterName;
        cmd.CommandText = cmd.CommandText.Replace($"@{baseParameterName}", $"${baseParameterName}");

        return param;
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
                    if (firstRow)
                        sbInsert.Append($" {propertyInfo.Name} ");

                    sbInsertValues.Append($" null ");

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

        List<PropertyInfo> propertiesToInsert = firstModel.GetPropertiesToInsert(conn, insertPrimaryKeyColumn: false, table);

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
