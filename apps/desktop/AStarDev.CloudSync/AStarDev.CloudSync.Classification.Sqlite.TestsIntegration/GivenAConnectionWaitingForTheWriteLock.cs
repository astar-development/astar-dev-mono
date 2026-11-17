using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

[Collection(SharedConnectionPool.Name)]
public sealed class GivenAConnectionWaitingForTheWriteLock : MigratedDatabaseTests
{
    [Fact]
    public async Task when_a_classification_store_has_used_a_connection_then_that_pooled_connection_waits_in_sqlite_for_a_busy_lock()
    {
        _ = await Database.Links.GetFileCountsAsync(CancellationToken);

        await using var pooled = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath }.ToString());
        await pooled.OpenAsync(CancellationToken);
        await using var command = pooled.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout;";
        var busyTimeout = await command.ExecuteScalarAsync(CancellationToken);

        busyTimeout.ShouldBe(5000L);
    }
}
