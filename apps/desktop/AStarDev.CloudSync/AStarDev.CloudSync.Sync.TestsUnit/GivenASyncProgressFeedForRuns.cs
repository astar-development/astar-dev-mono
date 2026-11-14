using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncProgressFeedForRuns : IDisposable
{
    private static readonly SyncTrigger Manual = SyncTriggerFactory.CreateManual();

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_run_goes_through_its_steps_then_each_phase_is_published_in_order()
    {
        List<string> phases = [];
        services.Feed.ProgressChanged += (_, change) => phases.Add(change.Progress.Phase.GetType().Name);
        services.Steps.OnStep = () => services.Time.Advance(TimeSpan.FromMilliseconds(300));

        _ = await services.Runner.RunAsync(services.Personal, Manual, CancellationToken);

        phases.ShouldBe(["UpdatingRemoteIndex", "ScanningLocal", "Downloading", "Uploading"]);
    }

    [Fact]
    public async Task when_phases_change_within_a_quarter_second_then_each_phase_is_still_published()
    {
        List<string> phases = [];
        services.Feed.ProgressChanged += (_, change) => phases.Add(change.Progress.Phase.GetType().Name);

        _ = await services.Runner.RunAsync(services.Personal, Manual, CancellationToken);

        phases.ShouldBe(["UpdatingRemoteIndex", "ScanningLocal", "Downloading", "Uploading"]);
    }

    [Fact]
    public async Task when_a_run_is_in_progress_then_the_account_has_progress_and_when_it_ends_it_has_none()
    {
        var gate = new WriteGate();
        services.Steps.DownloadGate = Option.Some(gate);
        services.Steps.OnStep = () => services.Time.Advance(TimeSpan.FromMilliseconds(300));
        var running = services.Runner.RunAsync(services.Personal, Manual, CancellationToken);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        var during = services.Feed.ProgressOf(services.Personal);
        gate.Release();
        _ = await running.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        during.Match(progress => progress.Phase.GetType().Name, () => "none").ShouldBe("Downloading");
        services.Feed.ProgressOf(services.Personal).ShouldBe(Option.None<SyncProgress>());
    }

    [Fact]
    public async Task when_a_second_run_starts_within_a_quarter_second_of_the_first_then_its_phases_are_published_again()
    {
        var published = 0;
        services.Feed.ProgressChanged += (_, _) => published++;

        _ = await services.Runner.RunAsync(services.Personal, Manual, CancellationToken);
        _ = await services.Runner.RunAsync(services.Personal, Manual, CancellationToken);

        published.ShouldBe(8);
    }

    [Fact]
    public async Task when_two_accounts_run_then_each_is_throttled_on_its_own()
    {
        var published = 0;
        services.Feed.ProgressChanged += (_, _) => published++;

        _ = await services.Runner.RunAsync(services.Personal, Manual, CancellationToken);
        _ = await services.Runner.RunAsync(services.Work, Manual, CancellationToken);

        published.ShouldBe(8);
    }

    public void Dispose() => services.Dispose();
}
