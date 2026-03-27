using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal sealed class AppDbContextFactory : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private AppDbContextFactory(SqliteConnection connection) => _connection = connection;

    public static AppDbContextFactory Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        return new AppDbContextFactory(connection);
    }

    public async Task<AppDbContext> CreateContextAsync(CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON", cancellationToken).ConfigureAwait(false);

        return context;
    }

    public static AppDbContext CreateForModelInspection()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        return new AppDbContext(options);
    }

    public SqliteConnection Connection => _connection;

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
