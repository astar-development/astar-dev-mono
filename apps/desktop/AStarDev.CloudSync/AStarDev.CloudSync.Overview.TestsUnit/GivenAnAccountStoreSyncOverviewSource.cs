using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    public async Task when_an_account_has_sync_pairs_then_the_overview_lists_them_and_the_accounts_counts_come_from_the_baseline_not_the_pairs()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var docs = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), FlintId, "Docs", RelativePaths.Of("Docs"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 0, Option.None<DateTimeOffset>());
        var photos = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), FlintId, "Photos", RelativePaths.Of("Photos"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateUploadOnly(), SyncStatusFactory.CreateUpToDate(), 0, Option.None<DateTimeOffset>());
        var reader = new FakeSyncPairReader { Pairs = accountId => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>(accountId == FlintId ? [docs, photos] : []) };

        var overview = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), reader).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, failure => throw new InvalidOperationException(failure.Message));

        overview.SyncPairs.Select(pair => pair.Name).ToList().ShouldBe(["Docs", "Photos"]);
        overview.Accounts.Single().FolderCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_an_account_has_baseline_counts_then_its_folder_and_file_counts_are_those_counts()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var counter = new FakeSyncBaselineCounter { Counts = accountId => Result.Success<BaselineCounts, ErrorResponse>(accountId == FlintId ? BaselineCountsFactory.Create(4, 250) : BaselineCountsFactory.Create(0, 0)) };

        var overview = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), new FakeAccountPauseReader(), counter).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, failure => throw new InvalidOperationException(failure.Message));

        overview.Accounts.ShouldHaveSingleItem().ShouldSatisfyAllConditions(account => account.FolderCount.ShouldBe(4), account => account.FileCount.ShouldBe(250));
    }

    [Fact]
    public async Task when_several_accounts_exist_then_each_gets_its_own_counts_and_none_loads_the_baseline()
    {
        var other = AccountIdFactory.Create(Guid.Parse("1f2e3d4c-5b6a-4798-8a9b-0c1d2e3f4a5b"));
        var otherAccount = StoredAccountFactory.Create(other, StoredFlint().Registration);
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint(), otherAccount]) };
        var counter = new FakeSyncBaselineCounter { Counts = accountId => Result.Success<BaselineCounts, ErrorResponse>(accountId == FlintId ? BaselineCountsFactory.Create(1, 2) : BaselineCountsFactory.Create(3, 4)) };

        var overview = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), new FakeAccountPauseReader(), counter).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, failure => throw new InvalidOperationException(failure.Message));

        overview.Accounts.Select(account => (account.Id, account.FolderCount, account.FileCount)).ShouldBe([(FlintId, 1, 2), (other, 3, 4)]);
        counter.Counted.ShouldBe([FlintId, other], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_baseline_cannot_be_counted_then_the_overview_fails_with_that_error_rather_than_showing_zero_counts()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var counter = new FakeSyncBaselineCounter { Counts = _ => Result.Failure<BaselineCounts, ErrorResponse>(new ErrorResponse("The baseline could not be counted.")) };

        var error = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), new FakeAccountPauseReader(), counter).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        error.ShouldBe("The baseline could not be counted.");
    }

    [Fact]
    public async Task when_the_overview_is_read_with_a_cancelled_token_then_it_is_cancelled()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var counter = new FakeSyncBaselineCounter { Counts = _ => throw new OperationCanceledException() };
        var read = () => CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), new FakeAccountPauseReader(), counter).GetOverviewAsync(TestContext.Current.CancellationToken);

        await Should.ThrowAsync<OperationCanceledException>(read);
    }

    [Fact]
    public async Task when_the_sync_pairs_cannot_be_read_then_the_overview_fails_with_that_error_rather_than_showing_no_pairs()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var reader = new FakeSyncPairReader { Pairs = _ => Result.Failure<IReadOnlyList<SyncPair>, ErrorResponse>(new ErrorResponse("The selection rules could not be read.")) };

        var error = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), reader).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        error.ShouldBe("The selection rules could not be read.");
    }

    [Fact]
    public async Task when_the_activity_log_has_entries_then_the_overview_carries_them_newest_first_as_read()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var older = ActivityEntryFactory.Create(FlintId, Option.None<SyncPairId>(), ActivityKindFactory.CreateDeleted(2), new DateTimeOffset(2026, 11, 14, 8, 0, 0, TimeSpan.Zero));
        var newer = ActivityEntryFactory.Create(FlintId, Option.None<SyncPairId>(), ActivityKindFactory.CreateDownloaded(1), new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
        var reader = new FakeActivityLogReader { Entries = Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>([newer, older]) };

        var overview = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), new FakeAccountPauseReader(), reader).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, failure => throw new InvalidOperationException(failure.Message));

        overview.Activity.ShouldBe([newer, older]);
        reader.Requests.ShouldHaveSingleItem().Count.ShouldBe(OverviewActivityLimit.EntryCount);
        reader.Requests.Single().AccountId.Match(_ => "one account", () => "all accounts").ShouldBe("all accounts");
    }

    [Fact]
    public async Task when_the_activity_log_cannot_be_read_then_the_overview_still_loads_with_an_empty_feed_and_a_warning_free_of_details()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var reader = new FakeActivityLogReader { Entries = Result.Failure<IReadOnlyList<ActivityEntry>, ErrorResponse>(new ErrorResponse($"{FlintEmail} locked /home/jbarden/cloudsync.db")) };
        using var logs = new RecordingLoggerProvider();

        var overview = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), new FakeAccountPauseReader(), new FakeSyncBaselineCounter(), reader, logs).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, failure => throw new InvalidOperationException(failure.Message));

        overview.Accounts.ShouldHaveSingleItem();
        overview.Activity.ShouldBeEmpty();
        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        string.Join("\n", logs.Logger.Messages).ShouldContain("activity");
        string.Join("\n", logs.Logger.Messages).ShouldNotContain(FlintEmail);
        string.Join("\n", logs.Logger.Messages).ShouldNotContain("/home/jbarden");
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

    [Fact]
    public async Task when_an_account_needs_sign_in_then_the_overview_account_carries_that_state()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var states = new FakeAccountSignInStates();
        states.Record(FlintId, AccountSignInStateFactory.CreateSignInRequired());

        var overview = await LoadAsync(store, states);

        overview.Accounts.ShouldHaveSingleItem().SignIn.ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_an_account_was_never_checked_then_it_is_signed_in()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };

        var overview = await LoadAsync(store);

        overview.Accounts.ShouldHaveSingleItem().SignIn.ShouldBe(AccountSignInStateFactory.CreateSignedIn());
    }

    [Fact]
    public async Task when_an_account_ran_then_its_status_and_last_sync_come_from_the_run_status_source()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var runs = new FakeSyncRunStatusSource();
        var finished = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        runs.Set(FlintId, SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()), Option.Some(finished));

        var overview = await LoadAsync(store, runs, new FakeAccountPauseReader());

        overview.Accounts.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            account => account.Status.ShouldBe(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded())),
            account => account.LastSync.ShouldBe(Option.Some(finished)));
    }

    [Fact]
    public async Task when_an_account_is_paused_and_idle_then_its_status_is_paused()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var pauses = new FakeAccountPauseReader { Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([FlintId]) };

        var overview = await LoadAsync(store, new FakeSyncRunStatusSource(), pauses);

        overview.Accounts.ShouldHaveSingleItem().Status.ShouldBe(SyncStatusFactory.CreatePaused());
    }

    [Fact]
    public async Task when_a_paused_account_is_syncing_then_it_shows_syncing()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var runs = new FakeSyncRunStatusSource();
        runs.Set(FlintId, SyncStatusFactory.CreateSyncing(0), Option.None<DateTimeOffset>());
        var pauses = new FakeAccountPauseReader { Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([FlintId]) };

        var overview = await LoadAsync(store, runs, pauses);

        overview.Accounts.ShouldHaveSingleItem().Status.ShouldBe(SyncStatusFactory.CreateSyncing(0));
    }

    [Fact]
    public async Task when_a_paused_account_last_failed_then_the_failure_is_still_shown()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var runs = new FakeSyncRunStatusSource();
        runs.Set(FlintId, SyncStatusFactory.CreateCancelled(), Option.None<DateTimeOffset>());
        var pauses = new FakeAccountPauseReader { Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([FlintId]) };

        var overview = await LoadAsync(store, runs, pauses);

        overview.Accounts.ShouldHaveSingleItem().Status.ShouldBe(SyncStatusFactory.CreateCancelled());
    }

    [Fact]
    public async Task when_the_paused_accounts_cannot_be_read_then_the_overview_fails_with_that_error()
    {
        var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
        var pauses = new FakeAccountPauseReader { Paused = Result.Failure<IReadOnlyList<AccountId>, ErrorResponse>(new ErrorResponse("The paused accounts could not be read.")) };

        var error = await CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), new FakeSyncRunStatusSource(), pauses).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(_ => "loaded", failure => failure.Message);

        error.ShouldBe("The paused accounts could not be read.");
    }

    private static Task<SyncOverview> LoadAsync(FakeAccountStore store, ISyncRunStatusSource runs, IAccountPauseReader pauses) => CreateSource(new FakeCloudSyncDatabase(), store, new FakeAccountSignInStates(), new FakeSyncPairReader(), runs, pauses).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(overview => overview, failure => throw new InvalidOperationException(failure.Message));

    private static Task<SyncOverview> LoadAsync(FakeAccountStore store) => LoadAsync(store, new FakeAccountSignInStates());

    private static Task<SyncOverview> LoadAsync(FakeAccountStore store, FakeAccountSignInStates states) => CreateSource(new FakeCloudSyncDatabase(), store, states).GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(overview => overview, failure => throw new InvalidOperationException(failure.Message));

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store) => CreateSource(database, store, new FakeAccountSignInStates());

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store, IAccountSignInStates states) => CreateSource(database, store, states, new FakeSyncPairReader());

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store, IAccountSignInStates states, ISyncPairReader pairReader) => CreateSource(database, store, states, pairReader, new FakeSyncRunStatusSource(), new FakeAccountPauseReader());

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store, IAccountSignInStates states, ISyncPairReader pairReader, ISyncRunStatusSource runStatuses, IAccountPauseReader pauseReader) =>
        CreateSource(database, store, states, pairReader, runStatuses, pauseReader, new FakeSyncBaselineCounter(), new FakeActivityLogReader());

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store, IAccountSignInStates states, ISyncPairReader pairReader, ISyncRunStatusSource runStatuses, IAccountPauseReader pauseReader, ISyncBaselineCounter counter) =>
        CreateSource(database, store, states, pairReader, runStatuses, pauseReader, counter, new FakeActivityLogReader());

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store, IAccountSignInStates states, ISyncPairReader pairReader, ISyncRunStatusSource runStatuses, IAccountPauseReader pauseReader, IActivityLogReader activityReader) =>
        CreateSource(database, store, states, pairReader, runStatuses, pauseReader, new FakeSyncBaselineCounter(), activityReader);

    private static ISyncOverviewSource CreateSource(ICloudSyncDatabase database, IAccountStore store, IAccountSignInStates states, ISyncPairReader pairReader, ISyncRunStatusSource runStatuses, IAccountPauseReader pauseReader, ISyncBaselineCounter counter, IActivityLogReader activityReader, RecordingLoggerProvider? logs = null) =>
        new ServiceCollection()
            .AddLogging(logging => _ = logs is null ? logging.ClearProviders() : logging.ClearProviders().AddProvider(logs))
            .AddSingleton(counter)
            .AddSingleton(activityReader)
            .AddSingleton(database)
            .AddSingleton(store)
            .AddSingleton(states)
            .AddSingleton(pairReader)
            .AddSingleton(runStatuses)
            .AddSingleton(pauseReader)
            .AddSyncOverview()
            .BuildServiceProvider()
            .GetRequiredService<ISyncOverviewSource>();

    private static StoredAccount StoredFlint() =>
        StoredAccountFactory.Create(FlintId, AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create(FlintAccountKey).Match(key => key, error => throw new InvalidOperationException(error.Message)), CloudProviderFactory.CreateOneDrive(), FlintDisplayName, FlintEmail, DriveIdFactory.Create("8f2a1b3c4d5e6f70").Match(driveId => driveId, error => throw new InvalidOperationException(error.Message)), FlintStorage));
}
