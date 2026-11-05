using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.Overview;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.Overview;
using AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.Overview;

public class GivenAnAccountStoreSyncOverviewSource
{
    private static readonly AccountId FlintId = AccountIdFactory.Create(Guid.Parse("0b1c2d3e-4f50-4617-8293-a4b5c6d7e8f9"));

    [Fact]
    public async Task when_accounts_are_saved_then_each_becomes_an_up_to_date_never_synced_account_with_its_profile_and_storage()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };

        var overview = await LoadAsync(store);

        overview.Accounts.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            account => account.Id.ShouldBe(FlintId),
            account => account.DisplayName.ShouldBe(TestSignIns.DisplayName),
            account => account.Email.ShouldBe(TestSignIns.Email),
            account => account.Provider.ShouldBe(CloudProviderFactory.CreateOneDrive()),
            account => account.Storage.ShouldBe(TestSignIns.Drive.Storage),
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

        var error = await new AccountStoreSyncOverviewSource(new FakeCloudSyncDatabase(), store).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        error.ShouldBe("The accounts could not be read.");
    }

    [Fact]
    public async Task when_the_database_cannot_be_migrated_then_the_overview_fails_and_the_accounts_are_not_read()
    {
        var reads = 0;
        var store = new FakeAccountStore { All = () => { reads++; return Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]); } };
        var database = new FakeCloudSyncDatabase { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The CloudSync database could not be created or migrated.")) };

        var error = await new AccountStoreSyncOverviewSource(database, store).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        (error, reads).ShouldBe(("The CloudSync database could not be created or migrated.", 0));
    }

    [Fact]
    public async Task when_the_overview_is_read_then_the_database_is_migrated_first()
    {
        var database = new FakeCloudSyncDatabase();

        _ = await new AccountStoreSyncOverviewSource(database, new FakeAccountStore()).GetOverviewAsync(TestContext.Current.CancellationToken);

        database.Migrations.ShouldBe(1);
    }

    private static Task<SyncOverview> LoadAsync(FakeAccountStore store) => new AccountStoreSyncOverviewSource(new FakeCloudSyncDatabase(), store).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(overview => overview, failure => throw new InvalidOperationException(failure.Message));

    private static StoredAccount StoredFlint() =>
        StoredAccountFactory.Create(FlintId, AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create(TestSignIns.AccountKey).Match(key => key, error => throw new InvalidOperationException(error.Message)), CloudProviderFactory.CreateOneDrive(), TestSignIns.DisplayName, TestSignIns.Email, TestSignIns.Drive.DriveId, TestSignIns.Drive.Storage));
}
