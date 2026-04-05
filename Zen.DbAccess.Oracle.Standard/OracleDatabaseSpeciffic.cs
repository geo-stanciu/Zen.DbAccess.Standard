using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Oracle.Standard.Extensions;
using Zen.DbAccess.Standard.Constants;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Extensions;
using Zen.DbAccess.Standard.Helpers;
using Zen.DbAccess.Standard.Interfaces;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.Oracle.Standard;

public class OracleDatabaseSpeciffic : DbSpeciffic
{
    public override DbConnection CreateConnection()
    {
        return new OracleConnection();
    }

    public override DbDataAdapter CreateDataAdapter(IZenDbConnection conn)
    {
        DbDataAdapter? da;

        if (conn.Connection is OracleConnection)
        {
            da = new OracleDataAdapter();
            (da as OracleDataAdapter)!.SuppressGetDecimalInvalidCastException = true;
        }
        else
            throw new NullReferenceException("DataAdapter");

        return da;
    }

    public override DbCommandBuilder CreateCommandBuilder(IZenDbConnection conn, DbDataAdapter dataAdapter)
    {
        DbCommandBuilder? builder = new OracleCommandBuilder();

        builder.DataAdapter = dataAdapter;

        return builder;
    }

    public override (string, SqlParam) PrepareEmptyParameter(DbModel model, PropertyInfo propertyInfo)
    {
        (string prmName, SqlParam prm) = ((IDbSpeciffic)this).CommonPrepareEmptyParameter(propertyInfo);

        if (model.IsClobDataType(propertyInfo))
        {
            prm.isClob = true;
        }
        else if (!prm.isBlob && model.IsBlobDataType(propertyInfo))
        {
            prm.isBlob = true;
        }

        return (prmName, prm);
    }

    public override (string, SqlParam) PrepareParameter(DbModel model, PropertyInfo propertyInfo)
    {
        (string prmName, SqlParam prm) = ((IDbSpeciffic)this).CommonPrepareParameter(model, propertyInfo);

        if (model.IsClobDataType(propertyInfo))
        {
            prm.isClob = true;
        }
        else if (!prm.isBlob && model.IsBlobDataType(propertyInfo))
        {
            prm.isBlob = true;
        }

        return (prmName, prm);
    }

    public override object GetValueForPreparedParameter(DbModel dbModel, PropertyInfo propertyInfo)
    {
        var val = propertyInfo.GetValue(dbModel) ?? DBNull.Value;

        return val!;
    }

    public override void DisposeBlob(DbCommand cmd, SqlParam prm)
    {
        if (prm.value != null && prm.value != DBNull.Value)
        {
            string baseParameterName = prm.name.StartsWith("@") ? prm.name.Substring(1) : prm.name;

            if (cmd.Parameters[baseParameterName].Value as OracleBlob != null)
                (cmd.Parameters[baseParameterName].Value as OracleBlob)!.Dispose();
        }
    }

    public override void DisposeClob(DbCommand cmd, SqlParam prm)
    {
        if (prm.value != null && prm.value != DBNull.Value)
        {
            string baseParameterName = prm.name.StartsWith("@") ? prm.name.Substring(1) : prm.name;

            if (cmd.Parameters[baseParameterName].Value as OracleClob != null)
                (cmd.Parameters[baseParameterName].Value as OracleClob)!.Dispose();
        }
    }

    public override bool ShouldSetDbTypeBinary()
    {
        return false;
    }

    public override object GetValueAsBlob(IZenDbConnection conn, object value)
    {
        OracleBlob blob = new OracleBlob(conn.Connection as OracleConnection);
        byte[] byteContent = (value as byte[])!;
        blob.Write(byteContent, 0, byteContent.Length);

        return blob;
    }

    public override object GetValueAsClob(IZenDbConnection conn, object value)
    {
        OracleClob clob = new OracleClob(conn.Connection as OracleConnection);
        byte[] byteContent = Encoding.Unicode.GetBytes((value as string)!);
        clob.Write(byteContent, 0, byteContent.Length);

        return clob;
    }

    public override DbCommand CreateCommand(IZenDbConnection conn)
    {
        var cmd = conn.Connection.CreateCommand();
        cmd.CommandTimeout = DbAccessConstants.DefaultCommandTimeoutSeconds;

        ((OracleCommand)cmd).BindByName = true;

        if (conn.Transaction != null)
            cmd.Transaction = conn.Transaction;

        return cmd;
    }

    public override DbParameter CreateDbParameter(DbCommand cmd, SqlParam prm)
    {
        DbParameter param = cmd.CreateParameter();

        string baseParameterName = prm.name.StartsWith("@") ? prm.name.Substring(1) : prm.name;
        param.ParameterName = baseParameterName;
        cmd.CommandText = cmd.CommandText.Replace($"@{baseParameterName}", $":{baseParameterName}");

        return param;
    }

    public override async Task InsertAsync(DbModel model, DbCommand cmd, bool insertPrimaryKeyColumn, DbModelSaveType saveType)
    {
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        if (insertPrimaryKeyColumn || saveType == DbModelSaveType.BulkInsertWithoutPrimaryKeyValueReturn)
        {
            return;
        }

        foreach (DbParameter prm in cmd.Parameters)
        {
            if (prm.Direction != ParameterDirection.Output)
                continue;

            if (model.HasPrimaryKey() && prm.Value != null && prm.Value != DBNull.Value)
            {
                var pkProp = model.GetPrimaryKeyProperties().First();

                if (pkProp.PropertyType == typeof(int))
                {
                    pkProp.SetValue(model, Convert.ToInt32(prm.Value), null);
                }
                else if (pkProp.PropertyType == typeof(long))
                {
                    pkProp.SetValue(model, Convert.ToInt64(prm.Value), null);
                }
                else
                {
                    pkProp.SetValue(model, prm.Value, null);
                }
            }

            break;
        }
    }

    public override void EnsureTempTable(string table)
    {
        string simplifiedName = table.IndexOf(".") > 0 ? table.Substring(table.IndexOf(".") + 1) : table;

        if (!simplifiedName.StartsWith("temp_", StringComparison.OrdinalIgnoreCase)
            && !simplifiedName.StartsWith("tmp_", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{table} must begin with temp_ or tmp_ .");
        }
    }

    public override void SetupFunctionCall(DbCommand cmd, string sql, params SqlParam[] parameters)
    {
        ((IDbSpeciffic)this).CommonSetupFunctionCall(cmd, sql, parameters);

        cmd.CommandText += " from dual";
    }

    public override string GetGetServerDateTimeQuery()
    {
        string sql = "SELECT sysdate from dual";

        return sql;
    }

    public override (string, IEnumerable<SqlParam>) GetInsertedIdQuery(string table, DbModel model, string firstPropertyName)
    {
        string sql;

        var pkProps = model.GetPrimaryKeyProperties();

        if (pkProps.Count == 1)
            sql = $" returning {model.GetMappedProperty(pkProps.First().Name)} into @p_out_id ";
        else
            sql = $" returning {model.GetMappedProperty(firstPropertyName)} into @p_out_id ";

        SqlParam prm = new SqlParam($"@p_out_id") { paramDirection = ParameterDirection.Output };

        return (sql, new[] { prm });
    }

    public override async Task BulkInsertAsync<T>(
        List<T> list,
        IZenDbConnection conn,
        string table,
        bool insertPrimaryKeyColumn = false)
    {
        T? firstModel = list.FirstOrDefault();

        if (firstModel == null)
            throw new NullReferenceException(nameof(firstModel));

        firstModel.RefreshDbColumnsAndModelProperties(conn, table);

        var propertiesToInsert = firstModel.GetPropertiesToInsert(conn, insertPrimaryKeyColumn, table);

        if (insertPrimaryKeyColumn || list.Count < 10_000)
        {
            if (list.Count > 10_000)
            {
                int offset = 0;

                while (offset < list.Count)
                {
                    var items = list.Skip(offset).Take(10_000).ToList();

                    await UseArrayBindingAsync(items, conn, table, firstModel, propertiesToInsert, insertPrimaryKeyColumn).ConfigureAwait(false);

                    offset += items.Count;
                }
            }
            else
            {
                await UseArrayBindingAsync(list, conn, table, firstModel, propertiesToInsert, insertPrimaryKeyColumn).ConfigureAwait(false);
            }

            return;
        }

        await UseBulkInsertAsync(list, conn, table, firstModel, propertiesToInsert).ConfigureAwait(false);
    }

    private async Task UseArrayBindingAsync<T>(List<T> list, IZenDbConnection conn, string table, T firstModel, List<PropertyInfo> propertiesToInsert, bool insertPrimaryKeyColumn) where T : DbModel
    {
        object[][] paramsArrays = new object[propertiesToInsert.Count][];
        OracleParameter[] sqlParams = new OracleParameter[propertiesToInsert.Count];

        int k = 0;

        var sbSql = new StringBuilder();
        var sbSqlValues = new StringBuilder();

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
                sbSqlValues.Append(", ");
            }

            sbSql.Append(dbCol);

            var prmName = $"p_{property.Name}";

            sbSqlValues.Append(":").Append(prmName);

            paramsArrays[k] = new object[list.Count];

            var dbType = GetCorrespondingOraclePropertyType<T>(firstModel, property);
            var oParam = new OracleParameter(prmName, dbType);
            oParam.Value = paramsArrays[k];

            if (insertPrimaryKeyColumn && firstModel.IsPartOfThePrimaryKey(dbCol!))
            {
                oParam.Direction = ParameterDirection.InputOutput;

                if (dbType == OracleDbType.Varchar2 || dbType == OracleDbType.NVarchar2)
                    oParam.Size = 4000;
            }

            sqlParams[k] = oParam;

            k++;
        }

        sbSql.Append(") VALUES (").Append(sbSqlValues).Append(")");

        for (int i = 0; i < list.Count; i++)
        {
            for (k = 0; k < propertiesToInsert.Count; k++)
            {
                var val = propertiesToInsert[k].GetValue(list[i]);

                if (val == null)
                {
                    paramsArrays[k][i] = DBNull.Value;
                    continue;
                }

                Type t = Nullable.GetUnderlyingType(propertiesToInsert[k].PropertyType) ?? propertiesToInsert[k].PropertyType;

                if (t.IsEnum || t.IsSubclassOf(typeof(Enum)))
                {
                    paramsArrays[k][i] = (int)val;
                }
                else if (t == typeof(bool))
                {
                    paramsArrays[k][i] = (bool)val ? 1 : 0;
                }
                else if (t == typeof(byte[]))
                {
                    paramsArrays[k][i] = GetValueAsBlob(conn, val);
                }
                else if (list[i].IsClobDataType(propertiesToInsert[k]))
                {
                    paramsArrays[k][i] = GetValueAsClob(conn, val);
                }
                else
                {
                    paramsArrays[k][i] = val;
                }
            }
        }

        using var cmd = new OracleCommand(sbSql.ToString(), (OracleConnection)conn.Connection);
        cmd.CommandTimeout = DbAccessConstants.DefaultCommandTimeoutSeconds;

        cmd.BindByName = true;

        if (conn.Transaction != null)
            cmd.Transaction = (OracleTransaction)conn.Transaction;

        cmd.ArrayBindCount = list.Count;

        cmd.Parameters.AddRange(sqlParams);

        _ = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        for (k = 0; k < cmd.Parameters.Count; k++)
        {
            var prm = cmd.Parameters[k];

            if (insertPrimaryKeyColumn)
            {
                var propName = firstModel.GetMappedProperty(prm.ParameterName.Substring(2));
                var dbCol = !string.IsNullOrEmpty(propName) ? firstModel.GetMappedProperty(propName) : null;
                var isPartOfThePrimaryKey = !string.IsNullOrEmpty(dbCol) ? firstModel.IsPartOfThePrimaryKey(dbCol) : false;

                if (isPartOfThePrimaryKey)
                {
                    var prop = propertiesToInsert.FirstOrDefault(x => x.Name == propName)
                        ?? throw new Exception($"Property {propName} not found in the properties to insert list for {table}");

                    for (int i = 0; i < list.Count; i++)
                    {
                        PropertyMapHelper.SetPropertyValue(list[i], prop, paramsArrays[k][i]);
                    }
                }
            }

            if (prm.OracleDbType == OracleDbType.Clob)
            {
                if (prm.Value != null)
                {
                    var clobs = prm.Value as object[];

                    for (int i = 0; i < clobs!.Length; i++)
                    {
                        if (clobs[i] != null && clobs[i] is OracleClob)
                            ((OracleClob)clobs[i]).Dispose();
                    }
                }
            }
            else if (prm.OracleDbType == OracleDbType.Blob)
            {
                if (prm.Value != null)
                {
                    var blobs = prm.Value as object[];

                    for (int i = 0; i < blobs!.Length; i++)
                    {
                        if (blobs[i] != null && blobs[i] is OracleBlob)
                            ((OracleBlob)blobs[i]).Dispose();
                    }
                }
            }

            prm.Dispose();
        }
    }

    private OracleDbType GetCorrespondingOraclePropertyType<T>(T model, PropertyInfo property) where T : DbModel
    {
        Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (propertyType.IsEnum || propertyType.IsSubclassOf(typeof(Enum)))
        {
            return OracleDbType.Int32;
        }
        else if (propertyType == typeof(bool))
        {
            return OracleDbType.Int32;
        }

        if (model.IsClobDataType(property))
            return OracleDbType.Clob;

        return propertyType switch
        {
            Type t when t == typeof(byte) => OracleDbType.Int32,
            Type t when t == typeof(short) => OracleDbType.Int16,
            Type t when t == typeof(int) => OracleDbType.Int32,
            Type t when t == typeof(long) => OracleDbType.Int64,
            Type t when t == typeof(float) => OracleDbType.Double,
            Type t when t == typeof(double) => OracleDbType.Double,
            Type t when t == typeof(decimal) => OracleDbType.Decimal,
            Type t when t == typeof(DateTime) => OracleDbType.TimeStamp,
            Type t when t == typeof(DateTimeOffset) => OracleDbType.TimeStampTZ,
            Type t when t == typeof(byte[]) => OracleDbType.Blob,
            Type t when t == typeof(string) => OracleDbType.Varchar2,
            _ => throw new NotImplementedException($"Type {propertyType} is not supported for bulk insert.")
        };
    }

    private async Task UseBulkInsertAsync<T>(List<T> list, IZenDbConnection conn, string table, T firstModel, List<PropertyInfo> propertiesToInsert) where T : DbModel
    {
        using OracleBulkCopy bulkCopy = new OracleBulkCopy((OracleConnection)conn.Connection);

        int idx = table.IndexOf(".");

        if (idx >= 0)
        {
            string schemaName = table.Substring(0, idx);
            string simplifiedTableName = table.Substring(idx + 1);

            bulkCopy.DestinationSchemaName = schemaName;
            bulkCopy.DestinationTableName = simplifiedTableName;
        }
        else
        {
            bulkCopy.DestinationTableName = table;
        }

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

            bulkCopy.ColumnMappings.Add(new OracleBulkCopyColumnMapping(k++, dbColName!));
        }

        foreach (var item in list)
        {
            var values = new object[propertiesToInsert.Count];

            k = 0;

            foreach (var property in propertiesToInsert)
            {
                var val = property.GetValue(item);

                if (val == null)
                {
                    values[k++] = DBNull.Value;
                    continue;
                }

                Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (t.IsEnum || t.IsSubclassOf(typeof(Enum)))
                {
                    values[k++] = (int)val;
                }
                else if (t == typeof(bool))
                {
                    values[k++] = (bool)val ? 1 : 0;
                }
                else
                {
                    values[k++] = val;
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
