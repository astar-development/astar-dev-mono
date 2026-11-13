using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenASyncRunOverviewReloader
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly CountingOverviewSource source = new();
    private readonly FakeSyncActivitySource activity = new();
    private readonly InMemorySyncOutcomes outcomes = new();
    private readonly ImmediateUiDispatcher dispatcher = new();

    public GivenASyncRunOverviewReloader() =>
        _ = new SyncRunOverviewReloader(activity, outcomes, new SyncOverviewViewModel(source, new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance), dispatcher);

    [Fact]
    public void when_a_sync_run_starts_then_the_overview_reloads_on_the_ui_thread()
    {
        activity.Start(Account, SyncTriggerFactory.CreateManual(), TestData.Now);

        (source.Reads, dispatcher.Invocations).ShouldBe((1, 1));
    }

    [Fact]
    public void when_a_sync_run_ends_then_the_overview_reloads()
    {
        activity.Finish(Account, SyncTriggerFactory.CreateManual(), Result.Failure<SyncPassSummary, SyncPassFailure>(SyncPassFailureFactory.CreateCancelled()));

        source.Reads.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_status_is_saved_then_the_overview_reloads()
    {
        _ = await outcomes.SaveAsync(Account, SyncStatusFactory.CreateUpToDate(), TestContext.Current.CancellationToken);

        source.Reads.ShouldBe(1);
    }

    private sealed class CountingOverviewSource : ISyncOverviewSource
    {
        public int Reads { get; private set; }

        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken)
        {
            Reads++;

            return Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([], [], [], [], [], 0, Option.None<DateTimeOffset>())));
        }
    }
}
