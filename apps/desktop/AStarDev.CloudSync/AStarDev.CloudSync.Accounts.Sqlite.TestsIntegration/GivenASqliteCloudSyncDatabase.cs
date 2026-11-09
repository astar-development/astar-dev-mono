using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

public class GivenASqliteCloudSyncDatabase
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_database_folder_does_not_exist_then_migration_creates_it_and_succeeds()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var migrated = await database.Database.MigrateAsync(CancellationToken);

        migrated.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_database_is_migrated_then_accounts_can_be_saved()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();
        await database.Database.MigrateAsync(CancellationToken);
        var registration = AccountRegistrationFactory.Create(new ProviderAccountKey("key"), CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("drive"), StorageUsageFactory.Create(0, 0));

        var outcome = await database.AccountStore.AddAsync(registration, CancellationToken);

        outcome.ShouldBeOfType<AccountAddOutcome.Added>();
    }

    [Fact]
    public async Task when_the_database_is_migrated_twice_then_the_second_migration_also_succeeds()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();
        await database.Database.MigrateAsync(CancellationToken);

        var migratedAgain = await database.Database.MigrateAsync(CancellationToken);

        migratedAgain.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_database_file_cannot_be_opened_then_migration_fails_and_logs_an_error()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateAtAFolder();

        var migrated = await database.Database.MigrateAsync(CancellationToken);

        migrated.Match(_ => true, _ => false).ShouldBeFalse();
        database.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == null);
    }
}
