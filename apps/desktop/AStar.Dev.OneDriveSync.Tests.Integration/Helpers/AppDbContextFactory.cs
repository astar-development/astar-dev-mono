using System;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal sealed class AppDbContextFactory : IAsyncDisposable
{
    private AppDbContextFactory(SqliteConnection connection) => Connection = connection;

    public static AppDbContextFactory Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        return new AppDbContextFactory(connection);
    }

    public async Task<AppDbContext> CreateContextAsync(CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection)
            .Options;

        var context = new AppDbContext(options);

        // Use MigrateAsync — never EnsureCreatedAsync — to exercise the real migration path (S002 AC).
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        _ = await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON", cancellationToken).ConfigureAwait(false);

        return context;
    }

    public static AppDbContext CreateForModelInspection()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        return new AppDbContext(options);
    }

    public SqliteConnection Connection { get; }

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync().ConfigureAwait(false);
}
