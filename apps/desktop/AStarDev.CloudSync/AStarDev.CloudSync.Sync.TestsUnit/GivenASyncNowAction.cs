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

    private int Runs => Steps.Steps.Count(step => step == "index");

    private Task SyncNowAsync(AccountId accountId) => services.SyncNow.SyncNowAsync(accountId, CancellationToken);

    private async Task<WriteGate> StartRunningAsync(AccountId accountId, SyncTrigger trigger)
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        _ = services.Runner.RunAsync(accountId, trigger, CancellationToken);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        return gate;
    }

    [Fact]
    public async Task when_the_account_is_idle_then_a_manual_run_goes_through_the_four_steps_without_asking()
    {
        await SyncNowAsync(Personal);

        Steps.Steps.ShouldBe(["index", "scan", "download", "upload"]);
        services.Dialog.Asked.ShouldBeEmpty();
        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_the_run_fails_then_sync_now_completes_and_the_status_shows_the_failure()
    {
        Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        await SyncNowAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateRootUnavailable()));
    }

    [Fact]
    public async Task when_a_scheduled_run_is_in_progress_then_the_user_is_asked_and_keeping_changes_nothing()
    {
        var gate = await StartRunningAsync(Personal, SyncTriggerFactory.CreateScheduled());
        services.Dialog.Choice = SyncRestartChoiceFactory.CreateKeepCurrent();

        await SyncNowAsync(Personal).WaitAsync(Limit, CancellationToken);
        gate.Release();
        await UntilIdleAsync(Personal);

        services.Dialog.Asked.ShouldBe([Personal]);
        Runs.ShouldBe(1);
        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_a_tray_run_is_in_progress_then_the_user_is_asked_before_anything_happens()
    {
        var gate = await StartRunningAsync(Personal, SyncTriggerFactory.CreateTray());

        await SyncNowAsync(Personal).WaitAsync(Limit, CancellationToken);
        gate.Release();

        services.Dialog.Asked.ShouldBe([Personal]);
        Runs.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_user_chooses_to_cancel_and_start_again_then_the_running_run_is_cancelled_and_a_manual_run_completes()
    {
        _ = await StartRunningAsync(Personal, SyncTriggerFactory.CreateScheduled());
        var finished = new List<SyncTrigger>();
        services.Activity.PassFinished += (_, args) => finished.Add(args.Trigger);
        services.Dialog.Choice = SyncRestartChoiceFactory.CreateCancelAndRestart();
        services.Dialog.BeforeAnswering = () => Steps.DownloadGate = Option.None<WriteGate>();

        await SyncNowAsync(Personal).WaitAsync(Limit, CancellationToken);

        Runs.ShouldBe(2);
        finished.Select(trigger => trigger.GetType()).ShouldBe([typeof(SyncTrigger.Scheduled), typeof(SyncTrigger.Manual)]);
        services.Activity.ActivityOf(Personal).ShouldBeOfType<AccountSyncActivity.Idle>();
        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_the_run_is_cancelled_and_restarted_then_the_cancelled_run_is_reported_as_cancelled_and_not_failed()
    {
        _ = await StartRunningAsync(Personal, SyncTriggerFactory.CreateScheduled());
        var outcomes = new List<Result<SyncPassSummary, SyncPassFailure>>();
        services.Activity.PassFinished += (_, args) => outcomes.Add(args.Outcome);
        services.Dialog.Choice = SyncRestartChoiceFactory.CreateCancelAndRestart();
        services.Dialog.BeforeAnswering = () => Steps.DownloadGate = Option.None<WriteGate>();

        await SyncNowAsync(Personal).WaitAsync(Limit, CancellationToken);

        outcomes[0].Match(_ => (object)"ran", failure => failure).ShouldBe(SyncPassFailureFactory.CreateCancelled());
    }

    [Fact]
    public async Task when_another_account_is_running_then_sync_now_runs_without_asking()
    {
        var gate = await StartRunningAsync(services.Work, SyncTriggerFactory.CreateScheduled());
        Steps.DownloadGate = Option.None<WriteGate>();

        await SyncNowAsync(Personal).WaitAsync(Limit, CancellationToken);
        gate.Release();

        services.Dialog.Asked.ShouldBeEmpty();
        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_twenty_sync_now_calls_race_for_one_account_then_exactly_one_pass_runs()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var racing = Enumerable.Range(0, 20).Select(_ => Task.Run(() => SyncNowAsync(Personal), CancellationToken)).ToArray();
        await gate.Entered.WaitAsync(Limit, CancellationToken);
        await Task.Delay(50, CancellationToken);

        gate.Release();
        await Task.WhenAll(racing).WaitAsync(Limit, CancellationToken);

        Runs.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_during_the_run_then_the_run_stops_as_cancelled()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var syncing = services.SyncNow.SyncNowAsync(Personal, source.Token);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        await source.CancelAsync();
        await syncing.WaitAsync(Limit, CancellationToken);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateCancelled());
        services.Activity.ActivityOf(Personal).ShouldBeOfType<AccountSyncActivity.Idle>();
    }

    [Fact]
    public async Task when_sync_now_runs_then_the_logs_carry_no_account_details()
    {
        await SyncNowAsync(Personal);

        services.Logs.Logger.Messages.ShouldNotContain(message => message.Contains(Personal.ToString()));
    }

    private async Task UntilIdleAsync(AccountId accountId)
    {
        while (services.Activity.ActivityOf(accountId) is not AccountSyncActivity.Idle)
            await Task.Delay(5, CancellationToken);
    }

    public void Dispose() => services.Dispose();
}
