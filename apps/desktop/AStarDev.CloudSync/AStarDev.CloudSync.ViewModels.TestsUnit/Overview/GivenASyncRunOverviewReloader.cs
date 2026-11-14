using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenASyncRunOverviewReloader
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public void when_a_run_starts_and_ends_then_the_overview_reloads_each_time()
    {
        var source = new CountingOverviewSource();
        var activity = new FakeSyncActivitySource();
        var overview = new SyncOverviewViewModel(source, new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SyncRunOverviewReloader(activity, overview, new ImmediateUiDispatcher());

        activity.RaiseStarted(Personal);
        activity.RaiseFinished(Personal);

        source.Reads.ShouldBe(2);
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
