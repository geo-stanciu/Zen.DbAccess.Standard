using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zen.DbAccess.Standard.Constants;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Factories;

namespace Zen.DbAccess.SqlServer.Standard.Factories;

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
