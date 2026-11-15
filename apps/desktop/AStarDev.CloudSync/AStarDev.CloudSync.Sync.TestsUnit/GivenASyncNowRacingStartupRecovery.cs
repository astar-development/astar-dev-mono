using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncNowRacingStartupRecovery : IDisposable
{
    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => services.Dispose();

    [Fact]
    public async Task when_recovery_reads_the_journal_while_a_run_is_in_flight_then_the_run_is_not_marked_interrupted()
    {
        var gate = new WriteGate();
        services.Steps.DownloadGate = Option.Some(gate);
        var running = services.Runner.RunAsync(services.Personal, SyncTriggerFactory.CreateManual(), CancellationToken);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        var outcomes = await services.Recovery.RecoverAsync(CancellationToken);
        gate.Release();
        _ = await running;

        (outcomes, services.Journal.Runs.ShouldHaveSingleItem().Outcome).ShouldBe(([], Option.Some(SyncRunOutcomeFactory.CreateCompleted())));
    }

    [Fact]
    public async Task when_recovery_reads_the_journal_between_the_row_being_written_and_the_run_being_registered_then_the_run_is_not_marked_interrupted()
    {
        var gate = new WriteGate();
        services.Journal.AfterRowAdded = () => gate.PassAsync();
        var running = services.Runner.RunAsync(services.Personal, SyncTriggerFactory.CreateManual(), CancellationToken);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        var recovering = services.Recovery.RecoverAsync(CancellationToken);
        await Task.Delay(100, CancellationToken);

        gate.Release();
        _ = await running;
        var outcomes = await recovering.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        outcomes.ShouldBeEmpty();
        services.Journal.Runs.ShouldHaveSingleItem().Outcome.ShouldBe(Option.Some(SyncRunOutcomeFactory.CreateCompleted()));
    }
}
