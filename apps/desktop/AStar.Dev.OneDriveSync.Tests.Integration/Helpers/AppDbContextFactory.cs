using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

/// <summary>
///     Creates a disposable, schema-initialised <see cref="AppDbContext" /> backed by a
///     SQLite in-memory database for integration tests.
///
///     Usage:
///     <code>
///     using var factory = AppDbContextFactory.Create();
///     await using var context = await factory.CreateContextAsync();
///     </code>
///
///     The underlying <see cref="SqliteConnection" /> is kept open for the lifetime of the
///     factory so that the in-memory database persists across multiple context instances
///     within the same test.
/// </summary>
internal sealed class AppDbContextFactory : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private AppDbContextFactory(SqliteConnection connection) => _connection = connection;

    /// <summary>Creates a new factory with a fresh, schema-initialised in-memory database.</summary>
    public static AppDbContextFactory Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        return new AppDbContextFactory(connection);
    }

    /// <summary>
    ///     Builds an <see cref="AppDbContext" /> pointed at the shared in-memory connection,
    ///     ensures the schema is created, and enables foreign-key enforcement via
    ///     <c>PRAGMA foreign_keys = ON</c> — SQLite disables FK constraints by default,
    ///     so this is required for cascade-delete and FK-violation tests to reflect real
    ///     database behaviour.  Each call returns a fresh context instance; all share the
    ///     same database because they share the same connection.
    /// </summary>
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

    /// <summary>Returns the open <see cref="SqliteConnection" /> for raw-SQL assertions.</summary>
    public SqliteConnection Connection => _connection;

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
