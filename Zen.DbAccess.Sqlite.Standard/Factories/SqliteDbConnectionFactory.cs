using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zen.DbAccess.Standard.Constants;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Factories;

namespace Zen.DbAccess.Sqlite.Standard.Factories;

public static class SqliteDbConnectionFactory
{
    public static DbConnectionFactory Create(
        string conn_str,
        bool commitNoWait = true,
        string timeZone = "",
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        return new DbConnectionFactory(
            DbConnectionType.Sqlite,
            conn_str,
            new SqliteDatabaseSpeciffic(),
            commitNoWait,
            timeZone,
            dbNamingConvention);
    }
}
