using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Overview.TestsUnit;

public class GivenOverviewServiceCollectionExtensions
{
    [Fact]
    public void when_the_sync_overview_is_added_then_one_source_is_shared_by_every_caller()
    {
        using var provider = CreateProvider();

        provider.GetRequiredService<ISyncOverviewSource>().ShouldBeSameAs(provider.GetRequiredService<ISyncOverviewSource>());
    }

    [Fact]
    public async Task when_the_overview_is_read_then_one_overview_refresh_run_is_counted_for_its_outcome()
    {
        using var provider = CreateProvider();
        var telemetry = provider.GetRequiredService<IWorkflowTelemetry>();
        var recorded = new List<string>();
        using var listener = new System.Diagnostics.Metrics.MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, telemetry) && instrument.Name == "cloudsync.workflow.runs")
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => recorded.Add(string.Join(',', tags.ToArray().Select(tag => $"{tag.Key}={tag.Value}").Order(StringComparer.Ordinal))));
        listener.Start();

        _ = await provider.GetRequiredService<ISyncOverviewSource>().GetOverviewAsync(TestContext.Current.CancellationToken);

        recorded.ShouldBe(["outcome=succeeded,workflow=overview_refresh"]);
    }

    private static ServiceProvider CreateProvider() => new ServiceCollection()
            .AddSingleton<ICloudSyncDatabase>(new FakeCloudSyncDatabase())
            .AddSingleton<IAccountStore>(new FakeAccountStore())
            .AddSingleton<IAccountSignInStates>(new FakeAccountSignInStates())
            .AddSingleton<ISyncPairReader>(new FakeSyncPairReader())
            .AddSingleton<ISyncRunStatusSource>(new FakeSyncRunStatusSource())
            .AddSingleton<IAccountPauseReader>(new FakeAccountPauseReader())
            .AddSingleton<ISyncBaselineCounter>(new FakeSyncBaselineCounter())
            .AddSingleton<IActivityLogReader>(new FakeActivityLogReader())
            .AddSingleton<ISyncAttentionReader>(new FakeAttentionReader())
            .AddSingleton<ISyncRunJournal>(new InMemorySyncRunJournal())
            .AddSingleton<IAccountThrottleHints>(new FakeAccountThrottleHints())
            .AddLogging()
            .AddSyncOverview()
            .BuildServiceProvider();
}
