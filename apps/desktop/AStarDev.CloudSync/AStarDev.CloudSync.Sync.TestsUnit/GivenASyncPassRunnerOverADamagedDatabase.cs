using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncPassRunnerOverADamagedDatabase : IDisposable
{
    private readonly InMemorySyncRunJournal journal = new();
    private readonly SyncRunServices services;

    public GivenASyncPassRunnerOverADamagedDatabase()
    {
        services = new SyncRunServices(journal);
        services.Health.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateDamaged());
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => services.Dispose();

    [Theory]
    [InlineData(nameof(SyncTrigger.Manual))]
    [InlineData(nameof(SyncTrigger.Scheduled))]
    [InlineData(nameof(SyncTrigger.Recovery))]
    public async Task when_the_integrity_check_failed_then_no_run_starts_for_any_trigger(string trigger)
    {
        var result = await services.Runner.RunAsync(services.Personal, TriggerNamed(trigger), CancellationToken);

        result.Match(_ => (object)"ran", failure => failure).ShouldBe(SyncPassFailureFactory.CreateDatabaseUnavailable());
        (services.Steps.Steps.Count, journal.Runs.Count).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_integrity_check_failed_then_the_run_does_not_touch_the_index_the_baseline_or_the_activity_log()
    {
        _ = await services.Runner.RunAsync(services.Personal, SyncTriggerFactory.CreateManual(), CancellationToken);

        (services.ActivityLog.Calls, services.Attention.Calls).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_database_is_healthy_then_a_run_goes_ahead()
    {
        services.Health.Answer = Result.Success<Unit, DatabaseHealthFailure>(Unit.Instance);

        var result = await services.Runner.RunAsync(services.Personal, SyncTriggerFactory.CreateManual(), CancellationToken);

        (result.Match(_ => true, _ => false), services.Steps.Steps.Count).ShouldBe((true, 4));
    }

    [Fact]
    public async Task when_many_runs_ask_while_the_database_is_damaged_then_the_check_runs_once_and_every_run_is_refused()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => services.Runner.RunAsync(services.Personal, SyncTriggerFactory.CreateScheduled(), CancellationToken), CancellationToken)));

        (services.Health.Calls, results.Count(result => result.Match(_ => false, failure => failure is SyncPassFailure.DatabaseUnavailable))).ShouldBe((1, 20));
    }

    private static SyncTrigger TriggerNamed(string name) => name switch
    {
        nameof(SyncTrigger.Manual) => SyncTriggerFactory.CreateManual(),
        nameof(SyncTrigger.Scheduled) => SyncTriggerFactory.CreateScheduled(),
        _ => SyncTriggerFactory.CreateRecovery(),
    };
}
