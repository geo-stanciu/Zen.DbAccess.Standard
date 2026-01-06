using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using Zen.DbAccess.Standard.Constants;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Factories;

namespace Zen.DbAccess.Postgresql.Standard.Factories;

public static class PostgresqlDbConnectionFactory
{
    public static DbConnectionFactory Create(
        string conn_str,
        bool commitNoWait = true,
        string timeZone = "",
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        return new DbConnectionFactory(
            DbConnectionType.Postgresql,
            conn_str,
            new PostgresqlDatabaseSpeciffic(),
            commitNoWait,
            timeZone,
            dbNamingConvention);
    }
}
