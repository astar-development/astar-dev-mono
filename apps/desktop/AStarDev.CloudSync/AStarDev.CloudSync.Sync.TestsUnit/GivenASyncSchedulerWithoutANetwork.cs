namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncSchedulerWithoutANetwork : IDisposable
{
    private readonly ScheduleServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan Minutes(int minutes) => TimeSpan.FromMinutes(minutes);

    public void Dispose() => services.Dispose();

    [Fact]
    public async Task when_a_pass_is_due_while_there_is_no_network_then_it_is_skipped()
    {
        _ = await services.AddAccountAsync();
        await services.Scheduler.StartAsync(CancellationToken);
        services.Network.Set(NetworkAvailabilityFactory.CreateUnavailable());

        await services.TickAtAsync(Minutes(2));
        await ScheduleServices.NothingElseHappensAsync();

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_network_returns_after_a_skipped_pass_then_exactly_one_pass_runs_at_once()
    {
        var account = await services.AddAccountAsync();
        await services.Scheduler.StartAsync(CancellationToken);
        services.Network.Set(NetworkAvailabilityFactory.CreateUnavailable());
        await services.TickAtAsync(Minutes(2));
        await ScheduleServices.NothingElseHappensAsync();
        var runsWhileOffline = services.Runner.Runs.Count;

        services.Network.Set(NetworkAvailabilityFactory.CreateAvailable());
        await services.WaitForRunsAsync(1);
        await ScheduleServices.NothingElseHappensAsync();

        runsWhileOffline.ShouldBe(0);
        services.Runner.Runs.Select(run => (run.AccountId, run.Trigger)).ShouldBe([(account, SyncTriggerFactory.CreateScheduled())]);
    }

    [Fact]
    public async Task when_the_network_is_down_for_several_intervals_then_only_one_pass_runs_when_it_returns()
    {
        _ = await services.AddAccountAsync();
        await services.Scheduler.StartAsync(CancellationToken);
        services.Network.Set(NetworkAvailabilityFactory.CreateUnavailable());
        await services.TickAtAsync(Minutes(2));
        await services.TickAtAsync(Minutes(70));
        await services.TickAtAsync(Minutes(140));
        await ScheduleServices.NothingElseHappensAsync();
        var runsWhileOffline = services.Runner.Runs.Count;

        services.Network.Set(NetworkAvailabilityFactory.CreateAvailable());
        await services.WaitForRunsAsync(1);
        await ScheduleServices.NothingElseHappensAsync();

        (runsWhileOffline, services.Runner.Runs.Count).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_the_network_returns_and_nothing_was_due_then_no_pass_runs()
    {
        _ = await services.AddAccountAsync();
        await services.Scheduler.StartAsync(CancellationToken);
        services.Network.Set(NetworkAvailabilityFactory.CreateUnavailable());
        await services.TickAtAsync(Minutes(1));

        services.Network.Set(NetworkAvailabilityFactory.CreateAvailable());
        await ScheduleServices.NothingElseHappensAsync();

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_network_returns_after_the_scheduler_stopped_then_nothing_runs_and_the_monitor_is_released()
    {
        _ = await services.AddAccountAsync();
        await services.Scheduler.StartAsync(CancellationToken);
        services.Network.Set(NetworkAvailabilityFactory.CreateUnavailable());
        await services.TickAtAsync(Minutes(2));
        await services.Scheduler.StopAsync(CancellationToken);

        services.Network.Set(NetworkAvailabilityFactory.CreateAvailable());
        await ScheduleServices.NothingElseHappensAsync();

        (services.Runner.Runs.Count, services.Network.Subscribers).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_network_stays_up_then_passes_run_on_time_as_before()
    {
        _ = await services.AddAccountAsync();
        await services.Scheduler.StartAsync(CancellationToken);

        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);

        services.Runner.Runs.Count.ShouldBe(1);
    }
}
