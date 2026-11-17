using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

[Collection(SharedConnectionPool.Name)]
public sealed class GivenAConnectionWaitingForTheWriteLock
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_account_store_has_used_a_connection_then_that_pooled_connection_waits_in_sqlite_for_a_busy_lock()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        _ = await database.AccountStore.GetAllAsync(CancellationToken);

        await using var pooled = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database.DatabaseFile.FullName }.ToString());
        await pooled.OpenAsync(CancellationToken);
        await using var command = pooled.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout;";
        var busyTimeout = await command.ExecuteScalarAsync(CancellationToken);

        busyTimeout.ShouldBe(5000L);
    }
}
