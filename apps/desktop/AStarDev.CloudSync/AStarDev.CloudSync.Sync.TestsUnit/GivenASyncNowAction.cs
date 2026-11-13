using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncNowAction : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ScriptedSyncSteps Steps => services.Steps;

    private AccountId Personal => services.Personal;

    [Fact]
    public async Task when_the_account_is_idle_then_a_manual_run_starts_without_asking()
    {
        var triggers = new List<SyncTrigger>();
        services.Activity.PassStarted += (_, args) => triggers.Add(args.Trigger);

        await services.SyncNow.SyncNowAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);

        triggers.ShouldBe([SyncTriggerFactory.CreateManual()]);
        services.Prompt.Asked.ShouldBeEmpty();
        Steps.Steps.ShouldBe(["index", "scan", "download", "upload"]);
    }

    [Fact]
    public async Task when_a_sync_is_running_and_the_user_keeps_it_then_nothing_changes()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var running = services.Runner.RunAsync(Personal, SyncTriggerFactory.CreateScheduled(), CancellationToken);
        await gate.Entered.WaitAsync(Limit, CancellationToken);
        services.Prompt.Choice = SyncRestartChoiceFactory.CreateKeepCurrent();

        await services.SyncNow.SyncNowAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);
        var stillRunning = services.Activity.ActivityOf(Personal);
        gate.Release();
        var first = await running.WaitAsync(Limit, CancellationToken);

        services.Prompt.Asked.ShouldBe([Personal]);
        stillRunning.ShouldBeOfType<AccountSyncActivity.Running>();
        first.Match(_ => "ran", failure => failure.GetType().Name).ShouldBe("ran");
        Steps.Steps.Count(step => step == "index").ShouldBe(1);
    }

    [Fact]
    public async Task when_a_sync_is_running_and_the_user_restarts_then_the_running_sync_is_cancelled_and_a_new_manual_run_completes()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var triggers = new List<SyncTrigger>();
        services.Activity.PassStarted += (_, args) => triggers.Add(args.Trigger);
        services.Activity.PassFinished += (_, _) => Steps.DownloadGate = Option.None<WriteGate>();
        var running = services.Runner.RunAsync(Personal, SyncTriggerFactory.CreateScheduled(), CancellationToken);
        await gate.Entered.WaitAsync(Limit, CancellationToken);
        services.Prompt.Choice = SyncRestartChoiceFactory.CreateCancelAndRestart();

        await services.SyncNow.SyncNowAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);
        var first = await running.WaitAsync(Limit, CancellationToken);

        first.Match(_ => (object)"ran", failure => failure).ShouldBe(SyncPassFailureFactory.CreateCancelled());
        triggers.ShouldBe([SyncTriggerFactory.CreateScheduled(), SyncTriggerFactory.CreateManual()]);
        services.Outcomes.Saved.Select(saved => saved.Status).ShouldBe([SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateCancelled()), SyncStatusFactory.CreateUpToDate()]);
        Steps.Steps.Count(step => step == "upload").ShouldBe(1);
    }

    [Fact]
    public async Task when_the_account_is_busy_with_other_work_then_the_user_is_not_asked_and_could_not_start_is_stored()
    {
        using var held = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(Personal)).Lease;

        await services.SyncNow.SyncNowAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);

        services.Prompt.Asked.ShouldBeEmpty();
        Steps.Steps.ShouldBeEmpty();
        services.Outcomes.Saved.ShouldBe([(Personal, SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateCouldNotStart()))]);
    }

    [Fact]
    public async Task when_sync_now_is_pressed_then_the_run_does_not_use_the_callers_thread()
    {
        var callerThread = Environment.CurrentManagedThreadId;
        var runThreads = new List<int>();
        services.Activity.PassStarted += (_, _) => runThreads.Add(Environment.CurrentManagedThreadId);

        var pressed = services.SyncNow.SyncNowAsync(Personal, CancellationToken);
        var startedOnCaller = runThreads.Contains(callerThread);
        await pressed.WaitAsync(Limit, CancellationToken);

        startedOnCaller.ShouldBeFalse();
        runThreads.Count.ShouldBe(1);
    }

    public void Dispose() => services.Dispose();
}
