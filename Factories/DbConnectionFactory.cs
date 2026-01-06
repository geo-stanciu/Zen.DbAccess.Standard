using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Extensions;
using Zen.DbAccess.Standard.Interfaces;
using Zen.DbAccess.Standard.Models;
using Zen.DbAccess.Standard.Utils;

namespace Zen.DbAccess.Standard.Factories;

public class DbConnectionFactory : IDbConnectionFactory
{
    private string? _connStr;
    private bool? _commitNoWait;
    private DbConnectionType? _dbType;
    private string? _timeZone;
    private IDbSpeciffic? _dbSpeciffic;
    private DbNamingConvention _dbNamingConvention;

    public DbConnectionFactory(
        DbConnectionType dbType,
        string conn_str,
        IDbSpeciffic? dbSpeciffic = null,
        bool commitNoWait = true,
        string timeZone = "",
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        _dbType = dbType;
        _connStr = conn_str;
        _commitNoWait = commitNoWait;
        _timeZone = timeZone;
        _dbSpeciffic = dbSpeciffic;
        _dbNamingConvention = dbNamingConvention;

        CreateDatabaseSpecifficIfNull();
    }

    private void CreateDatabaseSpecifficIfNull()
    {
        if (_dbSpeciffic != null)
        {
            return;
        }

        switch (_dbType)
        {
            case DbConnectionType.SqlServer:
                {
                    Type type = Type.GetType("Zen.DbAccess.SqlServer.Standard.SqlServerDatabaseSpeciffic, Zen.DbAccess.SqlServer.Standard");

                    if (type != null && Activator.CreateInstance(type) is IDbSpeciffic dbSpecifficInstance)
                    {
                        _dbSpeciffic = dbSpecifficInstance;
                        break;
                    }
                }
                break;
            case DbConnectionType.Postgresql:
                {
                    Type type = Type.GetType("Zen.DbAccess.Postgresql.Standard.PostgresqlDatabaseSpeciffic, Zen.DbAccess.Postgresql.Standard");

                    if (type != null && Activator.CreateInstance(type) is IDbSpeciffic dbSpecifficInstance)
                    {
                        _dbSpeciffic = dbSpecifficInstance;
                        break;
                    }
                }
                break;
            case DbConnectionType.Oracle:
                {
                    Type type = Type.GetType("Zen.DbAccess.Oracle.Standard.OracleDatabaseSpeciffic, Zen.DbAccess.Oracle.Standard");

                    if (type != null && Activator.CreateInstance(type) is IDbSpeciffic dbSpecifficInstance)
                    {
                        _dbSpeciffic = dbSpecifficInstance;
                        break;
                    }
                }
                break;
            case DbConnectionType.MariaDb:
                {
                    Type type = Type.GetType("Zen.DbAccess.MariaDb.Standard.MariaDbDatabaseSpeciffic, Zen.DbAccess.MariaDb.Standard");

                    if (type != null && Activator.CreateInstance(type) is IDbSpeciffic dbSpecifficInstance)
                    {
                        _dbSpeciffic = dbSpecifficInstance;
                        break;
                    }
                }
                break;
            case DbConnectionType.Sqlite:
                {
                    Type type = Type.GetType("Zen.DbAccess.Sqlite.Standard.SqliteDatabaseSpeciffic, Zen.DbAccess.Sqlite.Standard");

                    if (type != null && Activator.CreateInstance(type) is IDbSpeciffic dbSpecifficInstance)
                    {
                        _dbSpeciffic = dbSpecifficInstance;
                        break;
                    }
                }
                break;
            default:
                throw new NotImplementedException($"Database type {_dbType} is not supported");
        };
    }

    public DbConnectionType DbType
    {
        get
        {
            if (_dbType == null)
                throw new NullReferenceException(nameof(DbType));

            return _dbType.Value;
        }
        set
        {
            _dbType = value;
        }
    }

    public string? ConnectionString
    {
        get { return _connStr; }
        set { _connStr = value; }
    }

    public DbNamingConvention DbNamingConvention 
    {
        get => _dbNamingConvention;
        set => DbNamingConvention = value;
    }
    
    public IDbSpeciffic DatabaseSpeciffic
    {
        get => _dbSpeciffic!;
        set => _dbSpeciffic = value;
    }

    public string GenerateQueryColumns<T>() where T: DbModel
    {
        return _dbSpeciffic!.GenerateQueryColumns<T>(_dbType!.Value, _dbNamingConvention);
    }

    public IDbConnectionFactory Copy(string? newConnectionString = null)
    {
        if (_dbType == null)
            throw new NullReferenceException(nameof(_dbType));

        string? connString = newConnectionString;

        if (connString == null)
            connString = _connStr ?? string.Empty;

        return new DbConnectionFactory(_dbType.Value, connString, _dbSpeciffic, _commitNoWait ?? true, _timeZone ?? string.Empty, _dbNamingConvention);
    }

    public async Task<string> GenerateQueryColumnsAsync<T>() where T: DbModel
    {
        return _dbSpeciffic!.GenerateQueryColumns<T>(_dbType!.Value, _dbNamingConvention);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>A database connection object. The database connection is opened.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IZenDbConnection> BuildAsync()
    {
        DbConnection? conn = _dbSpeciffic!.CreateConnection();

        if (conn == null)
            throw new Exception($"Connection object is null for {_dbType} type");

        conn.ConnectionString = _connStr;

        ZenDbConnection connection = new ZenDbConnection(conn, _dbType!.Value, _dbSpeciffic, _dbNamingConvention);

        if (_dbType == DbConnectionType.Oracle)
        {
            await conn.OpenAsync();

            var sbSql = new StringBuilder();

            sbSql.Append("alter session set NLS_DATE_FORMAT='DD/MM/YYYY HH24:MI:SS' NLS_NUMERIC_CHARACTERS='.,' ");

            if (_commitNoWait!.Value)
            {
                sbSql.Append(" commit_logging=batch commit_wait=nowait ");
            }

            if (!string.IsNullOrEmpty(_timeZone))
            {
                sbSql.AppendLine($" time_zone='{_timeZone!.Replace("'", "''").Replace("&", "")}' ");
            }

            string sql = sbSql.ToString();

            await sql.ExecuteNonQueryAsync(connection);
        }
        else if (_dbType == DbConnectionType.Postgresql)
        {
            if (!string.IsNullOrEmpty(_timeZone) && !_connStr!.Contains(";Timezone=", StringComparison.OrdinalIgnoreCase))
            {
                _connStr += $"Timezone={_timeZone};";
            }

            await conn.OpenAsync();

            if (_commitNoWait!.Value)
            {
                string sql = "SET synchronous_commit = 'off'";
                await sql.ExecuteNonQueryAsync(connection);
            }
        }
        else if (_dbType == DbConnectionType.MariaDb)
        {
            await conn.OpenAsync();

            string sql = "SET SESSION sql_mode = 'ORACLE' ";
            await sql.ExecuteNonQueryAsync(connection);
        }
        else if (_dbType == DbConnectionType.Sqlite)
        {
            await conn.OpenAsync();

            string sql = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON; PRAGMA synchronous = NORMAL; ";
            await sql.ExecuteNonQueryAsync(connection);
        }

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        return connection;
    }

    public static DbConnectionFactory CreateFromConfiguration(
        IConfigurationManager configurationManager,
        string connectionStringName,
        DbConnectionType dbType,
        IDbSpeciffic? dbSpeciffic = null,
        bool commitNoWait = true,
        string? timeZone = null,
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        if (string.IsNullOrEmpty(connectionStringName))
            return GetDbConnectionFactoryFromConnectionSection(new ConnectionStringModel { DbType = $"{dbType}" }, dbSpeciffic, commitNoWait, dbNamingConvention);

        string? connString = configurationManager.GetConnectionString(connectionStringName);

        if (!string.IsNullOrEmpty(connString))
        {
            return GetDbConnectionFactoryWithConnectionString(connString!, dbType, dbSpeciffic, commitNoWait, timeZone);
        }

        ConnectionStringModel? connStringModel = configurationManager?
                .GetSection("DatabaseConnections")?
                .GetSection(connectionStringName)?
                .Get<ConnectionStringModel>();

        if (connStringModel == null)
            throw new NullReferenceException(nameof(connStringModel));

        return GetDbConnectionFactoryFromConnectionSection(connStringModel, dbSpeciffic, commitNoWait, dbNamingConvention);
    }

    public static DbConnectionFactory CreateFromConfiguration(
        IConfiguration configuration,
        string connectionStringName,
        DbConnectionType dbType,
        IDbSpeciffic? dbSpeciffic = null,
        bool commitNoWait = true,
        string? timeZone = null,
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        if (string.IsNullOrEmpty(connectionStringName))
            return GetDbConnectionFactoryFromConnectionSection(new ConnectionStringModel { DbType = $"{dbType}" }, dbSpeciffic, commitNoWait, dbNamingConvention);

        string? connString = configuration.GetConnectionString(connectionStringName);

        if (!string.IsNullOrEmpty(connString))
        {
            return GetDbConnectionFactoryWithConnectionString(connString!, dbType, dbSpeciffic, commitNoWait, timeZone);
        }

        ConnectionStringModel? connStringModel = configuration?
                .GetSection("DatabaseConnections")?
                .GetSection(connectionStringName)?
                .Get<ConnectionStringModel>();

        if (connStringModel == null)
            throw new NullReferenceException(nameof(connStringModel));

        return GetDbConnectionFactoryFromConnectionSection(connStringModel, dbSpeciffic, commitNoWait, dbNamingConvention);
    }

    private static DbConnectionFactory GetDbConnectionFactoryWithConnectionString(
        string connString,
        DbConnectionType dbType,
        IDbSpeciffic? dbSpeciffic = null,
        bool commitNoWait = true,
        string? timeZone = null,
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        DbConnectionFactory dbConnectionFactory = new DbConnectionFactory(
                dbType,
                connString,
                dbSpeciffic,
                commitNoWait,
                timeZone ?? "UTC",
                dbNamingConvention
            );

        return dbConnectionFactory;
    }

    private static DbConnectionFactory GetDbConnectionFactoryFromConnectionSection(
        ConnectionStringModel connStringModel,
        IDbSpeciffic? dbSpeciffic = null,
        bool commitNoWait = true,
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        DbConnectionFactory dbConnectionFactory = new DbConnectionFactory(
            connStringModel.DbConnectionType,
            connStringModel.ConnectionString,
            dbSpeciffic,
            commitNoWait,
            connStringModel.TimeZone,
            dbNamingConvention
        );

        return dbConnectionFactory;
    }
}
