using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Attributes;
using Zen.DbAccess.Constants;
using Zen.DbAccess.Enums;
using Zen.DbAccess.Helpers;
using Zen.DbAccess.Interfaces;
using Zen.DbAccess.Models;

namespace Zen.DbAccess.DatabaseSpeciffic;

public interface IDbSpeciffic
{
    DbConnection CreateConnection();

    DbCommandBuilder CreateCommandBuilder(IZenDbConnection conn, DbDataAdapter dataAdapter);

    char EscapeCustomNameStartChar();

    char EscapeCustomNameEndChar();

    string GetDbColumnNameForProperty<T>(
        IZenDbConnection conn,
        T model,
        PropertyInfo property,
        Dictionary<string, List<DbNameAttribute>> customColumnNames,
        char? startQuoteMark = null,
        char? endQuoteMark = null
        ) where T : DbModel;

    string GetDbColumnNameForProperty<T>(
        DbConnectionType dbType,
        DbNamingConvention namingConvention,
        PropertyInfo property,
        Dictionary<string, List<DbNameAttribute>> customColumnNames,
        char? startQuoteMark = null,
        char? endQuoteMark = null
        ) where T : DbModel;

    string GetSnakeCaseColumnName(string propName);

    string GetCamelCaseColumnName(string propName, char? startQuoteMark = null, char? endQuoteMark = null);

    string GetUseQuoteMarkesColumnName(
        string propName,
        char? startQuoteMark,
        char? endQuoteMark);

    string GenerateQueryColumns<T>(IZenDbConnection conn, T model) where T : DbModel;

    string GenerateQueryColumns<T>(IZenDbConnection conn) where T : DbModel;

    string GenerateQueryColumns<T>(DbConnectionType dbType, DbNamingConvention namingConvention, T model) where T : DbModel;

    string GenerateQueryColumns<T>(DbConnectionType dbType, DbNamingConvention namingConvention) where T : DbModel;

    (string, SqlParam) CommonPrepareEmptyParameter(PropertyInfo propertyInfo);

    (string, SqlParam) CommonPrepareParameter(DbModel model, PropertyInfo propertyInfo);

    (string, SqlParam) PrepareEmptyParameter(DbModel model, PropertyInfo propertyInfo);

    (string, SqlParam) PrepareParameter(DbModel model, PropertyInfo propertyInfo);

    void DisposeBlob(DbCommand cmd, SqlParam prm);

    void DisposeClob(DbCommand cmd, SqlParam prm);

    bool ShouldSetDbTypeBinary();

    object GetValueAsBlob(IZenDbConnection conn, object value);

    bool IsBlob(object value);

    object GetValueAsClob(IZenDbConnection conn, object value);

    DbParameter CreateDbParameter(DbCommand cmd, SqlParam prm);

    DbDataAdapter CreateDataAdapter(IZenDbConnection conn);

    bool UsePrimaryKeyPropertyForInsert();

    Task InsertAsync(DbModel model, DbCommand cmd, bool insertPrimaryKeyColumn, DbModelSaveType saveType);

    void EnsureTempTable(string table);

    void CommonSetupFunctionCall(DbCommand cmd, string sql, params SqlParam[] parameters);

    void SetupFunctionCall(DbCommand cmd, string sql, params SqlParam[] parameters);

    void SetupProcedureCall(IZenDbConnection conn, DbCommand cmd, string sql, bool isQueryReturn, params SqlParam[] parameters);

    bool ShouldFetchProcedureAsCursorsAsync();

    Task<List<T>> QueryCursorAsync<T>(IZenDbConnection conn, string procedureName, string cursorName);

    Task<List<string>> QueryCursorNamesAsync(IZenDbConnection conn, DbCommand cmd);

    Task<DataSet> ExecuteProcedure2DataSetAsync(IZenDbConnection conn, DbDataAdapter da);


    string GetGetServerDateTimeQuery();

    (string, IEnumerable<SqlParam>) GetInsertedIdQuery(string table, DbModel model, string firstPropertyName);

    Tuple<string, SqlParam[]> PrepareBulkInsertBatchWithSequence<T>(
       List<T> list,
       IZenDbConnection conn,
       string table,
       bool insertPrimaryKeyColumn,
       string sequence2UseForPrimaryKey) where T : DbModel;

    Tuple<string, SqlParam[]> PrepareBulkInsertBatch<T>(
        List<T> list,
        IZenDbConnection conn,
        string table) where T : DbModel;

    object GetValueForPreparedParameter(DbModel dbModel, PropertyInfo propertyInfo);
}
