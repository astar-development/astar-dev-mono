using AStarDev.CloudSync.Accounts;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

[Collection(SharedConnectionPool.Name)]
public sealed class GivenAConnectionWaitingForTheWriteLock : IAsyncLifetime
{
    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_a_sync_store_has_used_a_connection_then_that_pooled_connection_waits_in_sqlite_for_a_busy_lock()
    {
        _ = await Database.BaselineReader.GetAsync(AccountIdFactory.Create(Guid.NewGuid()), SyncPaths.Of("Docs/Report.docx"), CancellationToken);

        await using var pooled = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath }.ToString());
        await pooled.OpenAsync(CancellationToken);
        await using var command = pooled.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout;";
        var busyTimeout = await command.ExecuteScalarAsync(CancellationToken);

        busyTimeout.ShouldBe(5000L);
    }
}
