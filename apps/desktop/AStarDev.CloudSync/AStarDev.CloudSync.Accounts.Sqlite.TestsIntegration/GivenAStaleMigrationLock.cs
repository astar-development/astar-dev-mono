using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

public sealed class GivenAStaleMigrationLock
{
    private static readonly TimeSpan MigrationBudget = TimeSpan.FromSeconds(20);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task LeaveLockBehindAsync(TemporaryCloudSyncDatabase database)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database.DatabaseFile.FullName, Pooling = false }.ToString());
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS \"__EFMigrationsLock\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK___EFMigrationsLock\" PRIMARY KEY, \"Timestamp\" TEXT NOT NULL); INSERT OR IGNORE INTO \"__EFMigrationsLock\"(\"Id\", \"Timestamp\") VALUES(1, '2026-11-16 01:24:46+00:00');";
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<bool> MigratesWithinBudgetAsync(TemporaryCloudSyncDatabase database)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        budget.CancelAfter(MigrationBudget);

        return await Try.RunAsync(() => database.Database.MigrateAsync(budget.Token), budget.Token)
            .MatchAsync(result => result.Match(_ => true, _ => false), _ => false);
    }

    [Fact]
    public async Task when_a_lock_was_left_behind_then_clearing_it_lets_the_migration_finish()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        await LeaveLockBehindAsync(database);

        var cleared = await database.LockClearer.ClearAsync(CancellationToken);
        var migrated = await MigratesWithinBudgetAsync(database);

        (cleared.Match(_ => true, _ => false), migrated).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_there_is_no_lock_then_clearing_succeeds_and_the_migration_still_works()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);

        var cleared = await database.LockClearer.ClearAsync(CancellationToken);
        var migrated = await MigratesWithinBudgetAsync(database);

        (cleared.Match(_ => true, _ => false), migrated).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_the_database_does_not_exist_yet_then_clearing_succeeds_and_creates_nothing()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var cleared = await database.LockClearer.ClearAsync(CancellationToken);

        (cleared.Match(_ => true, _ => false), database.DatabaseFile.Exists).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_database_file_is_not_a_database_then_clearing_fails_with_an_error_and_not_an_exception()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();
        database.DatabaseFile.Directory!.Create();
        await File.WriteAllTextAsync(database.DatabaseFile.FullName, new string('x', 4096), CancellationToken);

        var cleared = await database.LockClearer.ClearAsync(CancellationToken);

        cleared.Match(_ => "ok", _ => "failed").ShouldBe("failed");
    }
}
