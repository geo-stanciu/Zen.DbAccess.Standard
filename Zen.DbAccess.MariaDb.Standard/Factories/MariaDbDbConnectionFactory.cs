using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;
using Zen.DbAccess.Standard.Constants;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Factories;

namespace Zen.DbAccess.MariaDb.Standard.Factories;

public static class MariaDbDbConnectionFactory
{
    public static DbConnectionFactory Create(
        string conn_str,
        bool commitNoWait = true,
        string timeZone = "",
        DbNamingConvention dbNamingConvention = DbNamingConvention.SnakeCase)
    {
        return new DbConnectionFactory(
            DbConnectionType.MariaDb,
            conn_str,
            new MariaDbDatabaseSpeciffic(),
            commitNoWait,
            timeZone,
            dbNamingConvention);
    }
}
