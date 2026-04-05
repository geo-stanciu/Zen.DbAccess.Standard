using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Standard.Factories;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Models;
using Zen.DbAccess.Standard.Interfaces;
using System.Text.Json;
using Newtonsoft.Json;

namespace Zen.DbAccess.Standard.Extensions;

public static class ListExtensions
{
    public static async Task SaveAllAsync<T>(
        this List<T> list,
        IDbConnectionFactory dbConnectionFactory,
        string table,
        bool runAllInTheSameTransaction = true,
        bool insertPrimaryKeyColumn = false) where T : DbModel
    {
        await using IZenDbConnection conn = await dbConnectionFactory.BuildAsync().ConfigureAwait(false);
        await list.SaveAllAsync(DbModelSaveType.InsertUpdate, conn, table, runAllInTheSameTransaction, insertPrimaryKeyColumn).ConfigureAwait(false);
    }

    public static async Task SaveAllAsync<T>(
        this List<T> list,
        DbModelSaveType dbModelSaveType,
        IDbConnectionFactory dbConnectionFactory,
        string table,
        bool runAllInTheSameTransaction = true,
        bool insertPrimaryKeyColumn = false) where T : DbModel
    {
        await using IZenDbConnection conn = await dbConnectionFactory.BuildAsync().ConfigureAwait(false);
        await list.SaveAllAsync(dbModelSaveType, conn, table, runAllInTheSameTransaction, insertPrimaryKeyColumn).ConfigureAwait(false);
    }

    public static async Task SaveAllAsync<T>(
        this List<T> list,
        IZenDbConnection conn,
        string table,
        bool runAllInTheSameTransaction = true,
        bool insertPrimaryKeyColumn = false) where T : DbModel
    {
        await list.SaveAllAsync(DbModelSaveType.InsertUpdate, conn, table, runAllInTheSameTransaction, insertPrimaryKeyColumn).ConfigureAwait(false);
    }

    public static async Task BulkInsertAsync<T>(
        this List<T> list,
        IZenDbConnection conn,
        string table, 
        bool runAllInTheSameTransaction = true,
        bool insertPrimaryKeyColumn = false) where T : DbModel
    {
        bool isInTransaction = conn.Transaction != null;

        if (runAllInTheSameTransaction && conn.Transaction == null)
            await conn.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            await conn.DatabaseSpeciffic.BulkInsertAsync<T>(list, conn, table, insertPrimaryKeyColumn).ConfigureAwait(false);
        }
        catch
        {
            if (!isInTransaction && conn.Transaction != null)
            {
                try
                {
                    await conn.RollbackAsync().ConfigureAwait(false);
                }
                catch { }
            }

            throw;
        }

        if (!isInTransaction && conn.Transaction != null)
            await conn.CommitAsync().ConfigureAwait(false);
    }

    public static async Task SaveAllAsync<T>(
        this List<T> list,
        DbModelSaveType dbModelSaveType,
        IZenDbConnection conn,
        string table,
        bool runAllInTheSameTransaction = true,
        bool insertPrimaryKeyColumn = false) where T : DbModel
    {
        bool isInTransaction = conn.Transaction != null;

        if (dbModelSaveType == DbModelSaveType.BulkInsertWithoutPrimaryKeyValueReturn)
        {
            if (insertPrimaryKeyColumn)
                throw new ArgumentException("insertPrimaryKeyColumn must be false when dbModelSaveType is BulkInsertWithoutPrimaryKeyValueReturn.");

            await BulkInsertAsync<T>(list, conn, table, runAllInTheSameTransaction, insertPrimaryKeyColumn: false).ConfigureAwait(false);
            return;
        }

        if (runAllInTheSameTransaction && conn.Transaction == null)
            await conn.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            T? firstModel = list.FirstOrDefault();

            if (firstModel == null)
                throw new NullReferenceException(nameof(firstModel));

            firstModel.RefreshDbColumnsAndModelProperties(conn, table);

            await firstModel.SaveAsync(dbModelSaveType, conn, table, insertPrimaryKeyColumn).ConfigureAwait(false);

            for (int i = 1; i < list.Count; i++)
            {
                T model = list[i];

                if (model == null)
                    continue;

                model.CopyDbModelPropsFrom(firstModel);
                await model.SaveAsync(dbModelSaveType, conn, table, insertPrimaryKeyColumn).ConfigureAwait(false);
            }
        }
        catch
        {
            if (!isInTransaction && conn.Transaction != null)
            {
                try
                {
                    await conn.RollbackAsync().ConfigureAwait(false);
                }
                catch { }
            }

            throw;
        }

        if (!isInTransaction && conn.Transaction != null)
            await conn.CommitAsync().ConfigureAwait(false);
    }

    public static async Task DeleteAllAsync<T>(
        this List<T> list,
        IDbConnectionFactory dbConnectionFactory,
        string table,
        bool runAllInTheSameTransaction = true) where T : DbModel
    {
        await using IZenDbConnection conn = await dbConnectionFactory.BuildAsync().ConfigureAwait(false);
        await DeleteAllAsync<T>(list, conn, table, runAllInTheSameTransaction).ConfigureAwait(false);
    }

    public static async Task DeleteAllAsync<T>(
        this List<T> list,
        IZenDbConnection conn,
        string table,
        bool runAllInTheSameTransaction = true) where T : DbModel
    {
        bool isInTransaction = conn.Transaction != null;

        if (runAllInTheSameTransaction && conn.Transaction == null)
            await conn.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            T? firstModel = list.FirstOrDefault();

            if (firstModel == null)
                throw new NullReferenceException(nameof(firstModel));

            firstModel.RefreshDbColumnsAndModelProperties(conn, table);

            if (firstModel.dbModel_primaryKey_dbColumns == null || firstModel.dbModel_primaryKey_dbColumns!.Count == 0)
                throw new NullReferenceException(nameof(firstModel.dbModel_primaryKey_dbColumns));

            (List<PropertyInfo> primaryKeyProps, bool isMultiColumnsPrimaryKey) = PreparePrimaryKeyProps4Delete<T>(firstModel);

            string sqlBase = PrepareDeleteBaseSql(firstModel, table, isMultiColumnsPrimaryKey);

            int offset = 0;
            int take = 512;

            while (offset < list.Count)
            {
                var items = list.Skip(offset).Take(take).ToList();
                offset += items.Count;

                (List<SqlParam> sqlParams, string deleteSqlList) = PrepareDeleteBulkSqlList<T>(items, isMultiColumnsPrimaryKey, primaryKeyProps);

                string sql = $" {sqlBase} in ( {deleteSqlList} ) ";

                _ = await sql.ExecuteNonQueryAsync(conn, sqlParams.ToArray()).ConfigureAwait(false);
            }
        }
        catch
        {
            if (!isInTransaction && conn.Transaction != null)
            {
                try
                {
                    await conn.RollbackAsync().ConfigureAwait(false);
                }
                catch { }
            }

            throw;
        }

        if (!isInTransaction && conn.Transaction != null)
            await conn.CommitAsync().ConfigureAwait(false);
    }

    private static (List<PropertyInfo>, bool) PreparePrimaryKeyProps4Delete<T>(T firstModel) where T : DbModel
    {
        List<PropertyInfo> primaryKeyProps = new List<PropertyInfo>();
        bool isMultiColumnsPrimaryKey = firstModel.dbModel_primaryKey_dbColumns!.Count > 1;

        foreach (string pkDbCol in firstModel.dbModel_primaryKey_dbColumns!)
        {
            primaryKeyProps.Add(firstModel.dbModel_dbColumn_map![pkDbCol]);
        }

        return (primaryKeyProps, isMultiColumnsPrimaryKey);
    }

    private static string PrepareDeleteBaseSql<T>(T firstModel, string table, bool isMultiColumnsPrimaryKey) where T : DbModel
    {
        StringBuilder sbSql = new StringBuilder();
        sbSql.Append($" delete from {table} where ");

        if (isMultiColumnsPrimaryKey)
        {
            bool isFirst = true;
            sbSql.Append("( ");

            foreach (string pkDbCol in firstModel.dbModel_primaryKey_dbColumns!)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sbSql.Append(", ");

                sbSql.Append($" {pkDbCol} ");
            }

            sbSql.Append(") ");
        }
        else
        {
            sbSql.Append($" {firstModel.dbModel_primaryKey_dbColumns!.First()} ");
        }

        return sbSql.ToString();
    }

    private static (List<SqlParam>, string) PrepareDeleteBulkSqlList<T>(IEnumerable<T> items, bool isMultiColumnsPrimaryKey, List<PropertyInfo> primaryKeyProps) where T : DbModel
    {
        List<SqlParam> sqlParams = new List<SqlParam>(items.Count() * primaryKeyProps.Count);
        StringBuilder sbDeleteSql = new StringBuilder();

        int k = 0;
        bool isFirstItem = true;

        foreach (var item in items)
        {
            if (isFirstItem)
                isFirstItem = false;
            else
                sbDeleteSql.Append(", ");

            if (isMultiColumnsPrimaryKey)
            {
                sbDeleteSql.Append("( ");

                bool isFirstProp = true;

                foreach (PropertyInfo pkProp in primaryKeyProps)
                {
                    if (isFirstProp)
                        isFirstProp = false;
                    else
                        sbDeleteSql.Append(", ");

                    string prmName = $"@p_{pkProp.Name}_{k}";
                    sbDeleteSql.Append($" {prmName} ");
                    sqlParams.Add(new SqlParam(prmName, pkProp.GetValue(item) ?? DBNull.Value));
                }

                sbDeleteSql.Append(") ");
            }
            else
            {
                PropertyInfo pkProp = primaryKeyProps.First();
                string prmName = $"@p_{pkProp.Name}_{k}";
                sbDeleteSql.Append($" {prmName} ");
                sqlParams.Add(new SqlParam(prmName, pkProp.GetValue(item) ?? DBNull.Value));
            }

            k++;
        }

        return (sqlParams, sbDeleteSql.ToString());
    }
    
    public static string ToJson<T>(this List<T> list)
    {
        return JsonConvert.SerializeObject(list);
    }

    public static string ToString<T>(this List<T> list)
    {
        return list.ToJson();
    }
}
