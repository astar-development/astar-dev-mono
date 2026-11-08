using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Overview.TestsUnit;

public class GivenAnAccountStoreSyncOverviewSource
{
    private const string FlintAccountKey = "00000000-0000-0000-4a1b-2c3d4e5f6a7b.9188040d-6c67-4c5b-b112-36a304b66dad";

    private const string FlintEmail = "captain.flint@example.com";

    private const string FlintDisplayName = "Captain Flint";

    private static readonly AccountId FlintId = AccountIdFactory.Create(Guid.Parse("0b1c2d3e-4f50-4617-8293-a4b5c6d7e8f9"));

    private static readonly StorageUsage FlintStorage = StorageUsageFactory.Create(1_500, 5_000);

    [Fact]
    public async Task when_accounts_are_saved_then_each_becomes_an_up_to_date_never_synced_account_with_its_profile_and_storage()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };

        var overview = await LoadAsync(store);

        overview.Accounts.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            account => account.Id.ShouldBe(FlintId),
            account => account.DisplayName.ShouldBe(FlintDisplayName),
            account => account.Email.ShouldBe(FlintEmail),
            account => account.Provider.ShouldBe(CloudProviderFactory.CreateOneDrive()),
            account => account.Storage.ShouldBe(FlintStorage),
            account => (account.FolderCount, account.FileCount).ShouldBe((0, 0)),
            account => account.Status.ShouldBeOfType<SyncStatus.UpToDate>(),
            account => account.LastSync.Match(_ => "synced", () => "never").ShouldBe("never"));
    }

    [Fact]
    public async Task when_the_overview_is_built_then_sync_pairs_activity_conflicts_and_throughput_are_empty()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };

        var overview = await LoadAsync(store);

        (overview.SyncPairs.Count, overview.Activity.Count, overview.Conflicts.Count, overview.Throughput.Count, overview.BytesMovedToday).ShouldBe((0, 0, 0, 0, 0L));
        overview.LastChecked.Match(_ => "checked", () => "never").ShouldBe("never");
    }

    [Fact]
    public async Task when_no_accounts_are_saved_then_the_overview_has_no_accounts()
    {
        var overview = await LoadAsync(new FakeAccountStore());

        overview.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_overview_fails_with_the_store_error()
    {
        var store = new FakeAccountStore { All = () => Result.Failure<IReadOnlyList<StoredAccount>, ErrorResponse>(new ErrorResponse("The accounts could not be read.")) };

        var error = await CreateSource(new FakeCloudSyncDatabase(), store).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        error.ShouldBe("The accounts could not be read.");
    }

    [Fact]
    public async Task when_the_database_cannot_be_migrated_then_the_overview_fails_and_the_accounts_are_not_read()
    {
        var reads = 0;
        var store = new FakeAccountStore { All = () => { reads++; return Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]); } };
        var database = new FakeCloudSyncDatabase { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The CloudSync database could not be created or migrated.")) };

        var error = await CreateSource(database, store).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        (error, reads).ShouldBe(("The CloudSync database could not be created or migrated.", 0));
    }

    [Fact]
    public async Task when_the_overview_is_read_then_the_database_is_migrated_first()
    {
        var database = new FakeCloudSyncDatabase();

        _ = await CreateSource(database, new FakeAccountStore()).GetOverviewAsync(TestContext.Current.CancellationToken);

        database.Migrations.ShouldBe(1);
    }

    private static Task<SyncOverview> LoadAsync(FakeAccountStore store) => CreateSource(new FakeCloudSyncDatabase(), store).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(overview => overview, failure => throw new InvalidOperationException(failure.Message));

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store) =>
        new ServiceCollection()
            .AddSingleton(database)
            .AddSingleton(store)
            .AddSyncOverview()
            .BuildServiceProvider()
            .GetRequiredService<ISyncOverviewSource>();

    private static StoredAccount StoredFlint() =>
        StoredAccountFactory.Create(FlintId, AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create(FlintAccountKey).Match(key => key, error => throw new InvalidOperationException(error.Message)), CloudProviderFactory.CreateOneDrive(), FlintDisplayName, FlintEmail, DriveIdFactory.Create("8f2a1b3c4d5e6f70").Match(driveId => driveId, error => throw new InvalidOperationException(error.Message)), FlintStorage));
}
