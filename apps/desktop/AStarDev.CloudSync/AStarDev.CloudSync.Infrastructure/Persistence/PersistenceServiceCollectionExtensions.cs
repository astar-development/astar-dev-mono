using System.IO.Abstractions;
using AStarDev.CloudSync.Core.Accounts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Infrastructure.Persistence;

/// <summary>Registers the CloudSync database and the stores built on it.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Registers the SQLite database at <paramref name="databaseFile"/> and the <see cref="IAccountStore"/>.</summary>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="databaseFile">The SQLite database file.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddCloudSyncPersistence(this IServiceCollection services, IFileInfo databaseFile) =>
        services
            .AddDbContextFactory<CloudSyncDbContext>(options => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = databaseFile.FullName }.ToString()))
            .AddSingleton<IAccountStore, SqliteAccountStore>();
}
