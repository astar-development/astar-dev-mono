using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenASyncRunOverviewReloader
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public void when_a_run_starts_and_ends_then_the_overview_reloads_each_time()
    {
        var source = new CountingOverviewSource();
        var activity = new FakeSyncActivitySource();
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SyncRunOverviewReloader(activity, new FakeSyncProgressFeed(), overview, new ImmediateUiDispatcher(), new FakeTimeProvider());

        activity.RaiseStarted(Personal);
        activity.RaiseFinished(Personal);

        source.Reads.ShouldBe(2);
    }

    [Fact]
    public void when_progress_is_published_then_the_overview_reloads_once_the_pause_has_passed()
    {
        var source = new CountingOverviewSource();
        var feed = new FakeSyncProgressFeed();
        var time = new FakeTimeProvider();
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SyncRunOverviewReloader(new FakeSyncActivitySource(), feed, overview, new ImmediateUiDispatcher(), time);

        feed.Raise(Personal, SyncProgressFactory.Create(SyncPhaseFactory.CreateDownloading(), 50, 100));
        var before = source.Reads;
        time.Advance(TimeSpan.FromSeconds(2));

        (before, source.Reads).ShouldBe((0, 1));
    }

    [Fact]
    public void when_many_progress_events_arrive_in_a_burst_then_the_overview_reloads_only_once()
    {
        var source = new CountingOverviewSource();
        var feed = new FakeSyncProgressFeed();
        var time = new FakeTimeProvider();
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SyncRunOverviewReloader(new FakeSyncActivitySource(), feed, overview, new ImmediateUiDispatcher(), time);

        foreach (var percent in Enumerable.Range(1, 20))
        {
            feed.Raise(Personal, SyncProgressFactory.Create(SyncPhaseFactory.CreateDownloading(), percent, 100));
            time.Advance(TimeSpan.FromMilliseconds(50));
        }

        time.Advance(TimeSpan.FromSeconds(2));

        source.Reads.ShouldBe(1);
    }

    [Fact]
    public void when_progress_arrives_again_after_a_reload_then_it_reloads_again()
    {
        var source = new CountingOverviewSource();
        var feed = new FakeSyncProgressFeed();
        var time = new FakeTimeProvider();
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SyncRunOverviewReloader(new FakeSyncActivitySource(), feed, overview, new ImmediateUiDispatcher(), time);

        feed.Raise(Personal, SyncProgressFactory.Create(SyncPhaseFactory.CreateDownloading(), 10, 100));
        time.Advance(TimeSpan.FromSeconds(2));
        feed.Raise(Personal, SyncProgressFactory.Create(SyncPhaseFactory.CreateDownloading(), 60, 100));
        time.Advance(TimeSpan.FromSeconds(2));

        source.Reads.ShouldBe(2);
    }

    [Fact]
    public void when_a_run_ends_while_progress_is_pending_then_it_reloads_at_once()
    {
        var source = new CountingOverviewSource();
        var feed = new FakeSyncProgressFeed();
        var activity = new FakeSyncActivitySource();
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SyncRunOverviewReloader(activity, feed, overview, new ImmediateUiDispatcher(), new FakeTimeProvider());

        feed.Raise(Personal, SyncProgressFactory.Create(SyncPhaseFactory.CreateDownloading(), 50, 100));
        activity.RaiseFinished(Personal);

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
