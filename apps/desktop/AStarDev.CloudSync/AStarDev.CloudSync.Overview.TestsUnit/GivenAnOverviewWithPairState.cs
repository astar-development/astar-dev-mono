using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Overview.TestsUnit;

public class GivenAnOverviewWithPairState
{
    private static readonly AccountId Flint = AccountIdFactory.Create(Guid.Parse("0b1c2d3e-4f50-4617-8293-a4b5c6d7e8f9"));
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task when_a_pair_has_synced_files_then_its_file_count_is_the_baseline_files_under_its_folder()
    {
        var fixture = new Fixture();
        fixture.Counter.FilesUnder = (_, folder) => Result.Success<int, ErrorResponse>(folder.Value == "Docs" ? 7 : 2);

        var overview = await fixture.ReadAsync();

        overview.SyncPairs.Select(pair => (pair.Name, pair.FileCount)).ShouldBe([("Docs", 7), ("Photos", 2)]);
    }

    [Fact]
    public async Task when_the_account_has_synced_then_every_pair_shows_the_accounts_last_sync()
    {
        var fixture = new Fixture();
        var run = (await fixture.Journal.StartAsync(Flint, SyncTriggerFactory.CreateScheduled(), TestContext.Current.CancellationToken)).Match(id => id, _ => throw new InvalidOperationException("The run must start."));
        _ = await fixture.Journal.FinishAsync(run, SyncRunOutcomeFactory.CreateCompleted(), TestContext.Current.CancellationToken);

        var overview = await fixture.ReadAsync();

        overview.SyncPairs.ShouldAllBe(pair => pair.LastSync.Match(time => time == Now, () => false));
    }

    [Fact]
    public async Task when_the_account_never_synced_then_the_pairs_show_never_synced()
    {
        var overview = await new Fixture().ReadAsync();

        overview.SyncPairs.ShouldAllBe(pair => pair.LastSync.Match(_ => false, () => true));
    }

    [Fact]
    public async Task when_nothing_is_known_then_a_pair_is_up_to_date_with_no_files()
    {
        var overview = await new Fixture().ReadAsync();

        overview.SyncPairs.ShouldAllBe(pair => pair.Status is SyncStatus.UpToDate && pair.FileCount == 0);
    }

    [Theory]
    [InlineData("syncing")]
    [InlineData("failed")]
    public async Task when_the_account_is_syncing_or_failed_then_its_pairs_show_that_state(string state)
    {
        var fixture = new Fixture();
        var status = state == "syncing" ? SyncStatusFactory.CreateSyncing(25) : SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded());
        fixture.Runs.Set(Flint, status);

        var overview = await fixture.ReadAsync();

        overview.SyncPairs.ShouldAllBe(pair => pair.Status == status);
    }

    [Fact]
    public async Task when_the_account_is_paused_then_its_pairs_show_paused()
    {
        var fixture = new Fixture { Paused = true };

        var overview = await fixture.ReadAsync();

        overview.SyncPairs.ShouldAllBe(pair => pair.Status is SyncStatus.Paused);
    }

    [Fact]
    public async Task when_conflicts_sit_under_one_pairs_folder_then_only_that_pair_shows_them_and_a_sibling_folder_does_not_count()
    {
        var fixture = new Fixture();
        fixture.Attention.Add(Flint, AttentionItems.CreateConflict(Flint, "docs/a.docx"), AttentionItems.CreateConflict(Flint, "Docs/Sub/b.docx"), AttentionItems.CreateConflict(Flint, "Docs2/c.docx"));

        var overview = await fixture.ReadAsync();

        overview.SyncPairs.Select(pair => (pair.Name, pair.Status)).ShouldBe([("Docs", SyncStatusFactory.CreateHasConflicts(2)), ("Photos", SyncStatusFactory.CreateUpToDate())]);
    }

    [Fact]
    public async Task when_a_files_count_cannot_be_read_then_the_pair_shows_no_files_and_a_warning_without_details_is_logged()
    {
        var fixture = new Fixture();
        fixture.Counter.FilesUnder = (_, _) => Result.Failure<int, ErrorResponse>(new ErrorResponse("secret-detail"));

        var overview = await fixture.ReadAsync();

        overview.SyncPairs.ShouldAllBe(pair => pair.FileCount == 0);
        string.Join("\n", fixture.Logs.Logger.Messages).ShouldContain("file count");
        string.Join("\n", fixture.Logs.Logger.Messages).ShouldNotContain("secret-detail");
    }

    private sealed class Fixture
    {
        public FakeSyncBaselineCounter Counter { get; } = new();

        public FakeAttentionReader Attention { get; } = new();

        public FakeSyncRunStatusSource Runs { get; } = new();

        public InMemorySyncRunJournal Journal { get; } = new(new FakeTimeProvider(Now));

        public RecordingLoggerProvider Logs { get; } = new();

        public bool Paused { get; init; }

        public async Task<SyncOverview> ReadAsync()
        {
            var pairs = new FakeSyncPairReader { Pairs = _ => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>([Pair("Docs"), Pair("Photos")]) };
            var store = new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredFlint()]) };
            await using var provider = new ServiceCollection()
                .AddSingleton<TimeProvider>(new FakeTimeProvider(Now))
                .AddSingleton<ICloudSyncDatabase>(new FakeCloudSyncDatabase())
                .AddSingleton<IAccountStore>(store)
                .AddSingleton<IAccountSignInStates>(new FakeAccountSignInStates())
                .AddSingleton<ISyncPairReader>(pairs)
                .AddSingleton<ISyncRunStatusSource>(Runs)
                .AddSingleton<IAccountPauseReader>(new FakeAccountPauseReader { Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>(Paused ? [Flint] : []) })
                .AddSingleton<ISyncBaselineCounter>(Counter)
                .AddSingleton<IActivityLogReader>(new FakeActivityLogReader())
                .AddSingleton<ISyncRunJournal>(Journal)
                .AddSingleton<IAccountThrottleHints>(new FakeAccountThrottleHints())
                .AddSingleton<ISyncAttentionReader>(Attention)
                .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
                .AddSyncOverview()
                .BuildServiceProvider();

            return await provider.GetRequiredService<ISyncOverviewSource>().GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(read => read, error => throw new InvalidOperationException(error.Message));
        }

        private static SyncPair Pair(string name) => SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), Flint, name, RelativePaths.Of(name), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncPairStateFactory.Create(SyncStatusFactory.CreateUpToDate(), 0, Option.None<DateTimeOffset>()));

        private static StoredAccount StoredFlint() =>
            StoredAccountFactory.Create(Flint, AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create("00000000-0000-0000-4a1b-2c3d4e5f6a7b.9188040d-6c67-4c5b-b112-36a304b66dad").Match(key => key, error => throw new InvalidOperationException(error.Message)), CloudProviderFactory.CreateOneDrive(), "Captain Flint", "captain.flint@example.com", DriveIdFactory.Create("8f2a1b3c4d5e6f70").Match(driveId => driveId, error => throw new InvalidOperationException(error.Message)), StorageUsageFactory.Create(1_500, 5_000)));
    }
}
