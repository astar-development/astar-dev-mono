using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncPassRunnerThatJournals : IDisposable
{
    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AccountId Personal => services.Personal;

    private static object Outcome(Result<SyncPassSummary, SyncPassFailure> result) => result.Match(_ => (object)"ran", failure => failure);

    private Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(SyncTrigger trigger) => services.Runner.RunAsync(Personal, trigger, CancellationToken);

    public void Dispose() => services.Dispose();

    [Fact]
    public async Task when_a_run_completes_then_one_row_is_journaled_with_its_trigger_times_and_the_completed_outcome()
    {
        var started = services.Time.GetUtcNow();

        _ = await RunAsync(SyncTriggerFactory.CreateScheduled());

        var run = services.Journal.Runs.ShouldHaveSingleItem();
        (run.AccountId, run.Trigger, run.Started, run.Outcome).ShouldBe((Personal, SyncTriggerFactory.CreateScheduled(), started, Option.Some(SyncRunOutcomeFactory.CreateCompleted())));
        run.Finished.ShouldBe(Option.Some(services.Time.GetUtcNow()));
    }

    [Fact]
    public async Task when_a_run_is_cancelled_then_the_row_ends_cancelled()
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        services.Steps.OnDownload = cancel.Cancel;

        var result = await services.Runner.RunAsync(Personal, SyncTriggerFactory.CreateManual(), cancel.Token);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateCancelled());
        services.Journal.Runs.ShouldHaveSingleItem().Outcome.ShouldBe(Option.Some(SyncRunOutcomeFactory.CreateCancelled()));
    }

    [Fact]
    public async Task when_a_run_fails_then_the_row_ends_failed_with_the_reason()
    {
        services.Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        _ = await RunAsync(SyncTriggerFactory.CreateManual());

        services.Journal.Runs.ShouldHaveSingleItem().Outcome.ShouldBe(Option.Some(SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateRootUnavailable())));
    }

    [Fact]
    public async Task when_a_step_throws_then_the_row_ends_failed_as_unexpected()
    {
        services.Steps.UploadThrows = Option.Some<Exception>(new InvalidOperationException("boom"));

        _ = await RunAsync(SyncTriggerFactory.CreateManual());

        services.Journal.Runs.ShouldHaveSingleItem().Outcome.ShouldBe(Option.Some(SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected())));
    }

    [Fact]
    public async Task when_a_run_is_skipped_because_another_is_running_then_no_row_is_journaled()
    {
        var gate = new WriteGate();
        services.Steps.DownloadGate = Option.Some(gate);
        var first = RunAsync(SyncTriggerFactory.CreateManual());
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        var second = await RunAsync(SyncTriggerFactory.CreateScheduled());
        gate.Release();
        _ = await first;

        Outcome(second).ShouldBe(SyncPassFailureFactory.CreateAlreadyRunning());
        services.Journal.Runs.ShouldHaveSingleItem().Trigger.ShouldBe(SyncTriggerFactory.CreateManual());
    }

    [Fact]
    public async Task when_the_journal_cannot_be_written_then_the_run_still_runs_and_a_warning_names_no_details()
    {
        services.Journal.Fails = true;

        var result = await RunAsync(SyncTriggerFactory.CreateManual());

        Outcome(result).ShouldBe("ran");
        services.Steps.Steps.ShouldBe(["index", "scan", "download", "upload"]);
        string.Join("\n", services.Logs.Logger.Messages).ShouldContain("run journal");
        string.Join("\n", services.Logs.Logger.Messages).ShouldNotContain("locked");
    }

    [Fact]
    public async Task when_a_run_ends_then_the_journal_is_written_after_the_activity_log_while_the_lease_is_still_held()
    {
        services.Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateDownloaded(10)], []));
        var activityLoggedBeforeFinish = false;
        var leaseFreeAtFinish = true;
        services.Journal.OnFinish = () =>
        {
            activityLoggedBeforeFinish = services.ActivityLog.Appended.Count > 0;
            leaseFreeAtFinish = services.SyncLock.TryAcquire(Personal) is SyncLockAttempt.Acquired;
        };

        _ = await RunAsync(SyncTriggerFactory.CreateManual());

        (activityLoggedBeforeFinish, leaseFreeAtFinish).ShouldBe((true, false));
    }
}
