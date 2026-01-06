using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Zen.DbAccess.Standard.DatabaseSpeciffic;
using Zen.DbAccess.Standard.Enums;
using Zen.DbAccess.Standard.Interfaces;
using Zen.DbAccess.Standard.Models;

namespace Zen.DbAccess.Standard.Factories;

public interface IDbConnectionFactory
{
    Task<IZenDbConnection> BuildAsync();
    DbConnectionType DbType { get; set; }
    string? ConnectionString { get; set; }
    DbNamingConvention DbNamingConvention { get; set; }
    IDbSpeciffic DatabaseSpeciffic { get; set; }
    string GenerateQueryColumns<T>() where T: DbModel;
    IDbConnectionFactory Copy(string? newConnectionString = null);
}