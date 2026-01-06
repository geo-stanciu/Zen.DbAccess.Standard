using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zen.DbAccess.Constants;
using Zen.DbAccess.Enums;
using Zen.DbAccess.Factories;

namespace Zen.DbAccess.SqlServer.Factories;

public static class ServerDbConnectionFactory
{
    public static DbConnectionFactory Create(
        string conn_str,
        bool commitNoWait = true,
        string timeZone = "",
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        return new DbConnectionFactory(
            DbConnectionType.SqlServer,
            conn_str,
            new SqlServerDatabaseSpeciffic(),
            commitNoWait,
            timeZone,
            dbNamingConvention);
    }
}
