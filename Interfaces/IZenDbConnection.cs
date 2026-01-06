using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Enums;

namespace Zen.DbAccess.Standard.Interfaces;

public interface IZenDbConnection : IAsyncDisposable
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
    DbConnectionType DbType { get; }
    IDbSpeciffic DatabaseSpeciffic { get; }
    DbNamingConvention NamingConvention { get; }


    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task CloseAsync();
}
