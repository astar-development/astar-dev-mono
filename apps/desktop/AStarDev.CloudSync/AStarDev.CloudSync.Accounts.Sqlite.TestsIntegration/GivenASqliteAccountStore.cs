using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

public sealed class GivenASqliteAccountStore : GivenAnAccountStore
{
    private const string HomeAccountIdentifier = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";

    private TemporaryCloudSyncDatabase? database;

    public override async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();

        await base.DisposeAsync();
    }

    protected override async Task<IAccountStore> CreateStoreAsync(CancellationToken cancellationToken)
    {
        database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(cancellationToken);

        return database.AccountStore;
    }

    [Fact]
    public async Task when_the_account_cannot_be_saved_then_the_outcome_is_persist_failed()
    {
        await using var unmigrated = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var outcome = await unmigrated.AccountStore.AddAsync(Registration(), CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed()));
    }

    [Fact]
    public async Task when_the_account_cannot_be_saved_then_an_error_is_logged()
    {
        await using var unmigrated = TemporaryCloudSyncDatabase.CreateUnmigrated();

        await unmigrated.AccountStore.AddAsync(Registration(), CancellationToken);

        unmigrated.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == null);
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_list_fails()
    {
        await using var unmigrated = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var all = await unmigrated.AccountStore.GetAllAsync(CancellationToken);

        all.Match(_ => true, _ => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_an_account_cannot_be_looked_up_then_the_lookup_fails()
    {
        await using var unmigrated = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var found = await unmigrated.AccountStore.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(), CancellationToken);

        found.Match(_ => true, _ => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_storage_cannot_be_saved_then_the_update_fails_and_an_error_is_logged_without_account_details()
    {
        await using var unmigrated = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var result = await unmigrated.AccountStore.UpdateStorageAsync(AccountIdFactory.Create(Guid.NewGuid()), StorageUsageFactory.Create(1, 2), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeFalse();
        unmigrated.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == null);
    }

    private static ProviderAccountKey Key() => ProviderAccountKeyFactory.Create(HomeAccountIdentifier).Match(key => key, _ => throw new InvalidOperationException("The test key must be valid."));

    private static AccountRegistration Registration() => AccountRegistrationFactory.Create(Key(), CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1_024, 5_368_709_120));
}
