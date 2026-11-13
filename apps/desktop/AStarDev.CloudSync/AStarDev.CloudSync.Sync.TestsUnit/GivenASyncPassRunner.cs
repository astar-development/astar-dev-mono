using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncPassRunner : IDisposable
{
    private static readonly SyncTrigger Manual = SyncTriggerFactory.CreateManual();
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ScriptedSyncSteps Steps => services.Steps;

    private AccountId Personal => services.Personal;

    private static object Outcome(Result<SyncPassSummary, SyncPassFailure> result) => result.Match(_ => (object)"ran", failure => failure);

    private Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(AccountId accountId, SyncTrigger trigger) => services.Runner.RunAsync(accountId, trigger, CancellationToken);

    [Fact]
    public async Task when_the_account_is_idle_then_the_four_steps_run_in_order()
    {
        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe("ran");
        Steps.Steps.ShouldBe(["index", "scan", "download", "upload"]);
    }

    [Fact]
    public async Task when_the_passes_send_paths_to_conflicts_and_hold_deletions_then_the_summary_counts_them()
    {
        var pair = SyncPairIdFactory.Create(Guid.NewGuid());
        var path = RelativeSyncPathFactory.Create("Docs/a.txt").Match(found => found, _ => throw new InvalidOperationException("The test path must be valid."));
        Steps.Scan = Result.Success<LocalChangeSet, LocalScanFailure>(LocalChangeSetFactory.Create([], [MassDeletionSuspectedFactory.Create(pair, 60, 120)], 0, 0));
        Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictKindFactory.CreateBothModified()), DownloadItemOutcomeFactory.CreateHeldByGuard()], []));
        Steps.Uploaded = Result.Success<UploadPassSummary, UploadPassFailure>(UploadPassSummaryFactory.Create([UploadItemOutcomeFactory.CreateSentToConflicts(path, ConflictKindFactory.CreateBothAdded()), UploadItemOutcomeFactory.CreateUploaded(path, 10)]));

        var result = await RunAsync(Personal, Manual);

        result.Match(summary => (summary.Conflicts, summary.HeldDeletions, summary.Upload.Uploaded), _ => (-1, -1, -1)).ShouldBe((2, 61, 1));
    }

    [Fact]
    public async Task when_the_remote_index_needs_sign_in_then_the_run_stops_and_the_account_is_marked_as_needing_sign_in()
    {
        Steps.Index = Result.Failure<RemoteUpdateSummary, ProviderError>(ProviderErrorFactory.CreateAuthenticationRequired());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateAuthenticationRequired());
        Steps.Steps.ShouldBe(["index"]);
        services.States.StateOf(Personal).ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_the_remote_index_fails_for_another_reason_then_the_run_is_unexpected_and_sign_in_is_unchanged()
    {
        Steps.Index = Result.Failure<RemoteUpdateSummary, ProviderError>(ProviderErrorFactory.CreateTransient());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
        Steps.Steps.ShouldBe(["index"]);
        services.States.StateOf(Personal).ShouldBe(AccountSignInStateFactory.CreateSignedIn());
    }

    [Fact]
    public async Task when_the_sync_root_is_missing_at_the_scan_then_the_run_stops_before_any_transfer()
    {
        Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateRootUnavailable());
        Steps.Steps.ShouldBe(["index", "scan"]);
    }

    [Fact]
    public async Task when_the_scan_fails_for_another_reason_then_the_run_is_unexpected()
    {
        Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateUnexpected(new ErrorResponse("scan failed")));

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
        Steps.Steps.ShouldBe(["index", "scan"]);
    }

    [Fact]
    public async Task when_the_disk_is_full_during_downloads_then_the_whole_run_stops_and_no_upload_runs()
    {
        Steps.Downloaded = Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateInsufficientDiskSpace());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateInsufficientDiskSpace());
        Steps.Steps.ShouldBe(["index", "scan", "download"]);
    }

    [Fact]
    public async Task when_the_download_pass_needs_sign_in_then_the_account_is_marked_and_no_upload_runs()
    {
        Steps.Downloaded = Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateAuthenticationRequired());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateAuthenticationRequired());
        Steps.Steps.ShouldNotContain("upload");
        services.States.StateOf(Personal).ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_the_download_pass_finds_no_root_then_the_run_reports_root_unavailable()
    {
        Steps.Downloaded = Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateRootUnavailable());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateRootUnavailable());
    }

    [Fact]
    public async Task when_the_download_pass_fails_unexpectedly_then_the_run_is_unexpected()
    {
        Steps.Downloaded = Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateUnexpected());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
    }

    [Fact]
    public async Task when_the_provider_is_full_then_quota_exceeded_is_reported_after_the_downloads_ran()
    {
        Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateQuotaExceeded());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateQuotaExceeded());
        Steps.Steps.ShouldBe(["index", "scan", "download", "upload"]);
    }

    [Fact]
    public async Task when_the_upload_pass_needs_sign_in_then_the_account_is_marked_as_needing_sign_in()
    {
        Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateAuthenticationRequired());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateAuthenticationRequired());
        services.States.StateOf(Personal).ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_the_upload_pass_finds_no_root_then_the_run_reports_root_unavailable()
    {
        Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateRootUnavailable());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateRootUnavailable());
    }

    [Fact]
    public async Task when_the_upload_pass_fails_unexpectedly_then_the_run_is_unexpected()
    {
        Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateUnexpected());

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
    }

    [Fact]
    public async Task when_a_step_throws_then_the_run_is_unexpected_the_account_is_freed_and_the_log_has_no_exception_text()
    {
        Steps.DownloadThrows = Option.Some<Exception>(new InvalidOperationException("jane@example.com secret-token /home/jane/Docs"));

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
        services.SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>();
        var logged = string.Join("\n", services.Logs.Logger.Messages);
        logged.ShouldContain(nameof(InvalidOperationException));
        logged.ShouldNotContain("jane@example.com");
        logged.ShouldNotContain("secret-token");
        logged.ShouldNotContain("/home/jane");
    }

    [Fact]
    public async Task when_a_run_ends_then_the_log_names_the_trigger_and_never_the_account_id()
    {
        _ = await RunAsync(Personal, SyncTriggerFactory.CreateTray());

        var logged = string.Join("\n", services.Logs.Logger.Messages);
        logged.ShouldContain("Tray");
        logged.ShouldNotContain(Personal.Value.ToString());
    }

    [Fact]
    public async Task when_a_run_succeeds_then_the_account_is_free_for_the_next_run()
    {
        _ = await RunAsync(Personal, Manual);

        var second = await RunAsync(Personal, Manual);

        Outcome(second).ShouldBe("ran");
        Steps.Steps.Count(step => step == "download").ShouldBe(2);
    }

    [Fact]
    public async Task when_a_run_fails_then_the_account_is_free_for_the_next_run()
    {
        Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        _ = await RunAsync(Personal, Manual);

        services.SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    [Fact]
    public async Task when_a_scheduled_trigger_hits_a_running_manual_run_then_it_is_skipped_logged_and_only_one_pass_runs()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var manual = RunAsync(Personal, Manual);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        var scheduled = await RunAsync(Personal, SyncTriggerFactory.CreateScheduled()).WaitAsync(Limit, CancellationToken);
        gate.Release();
        var first = await manual.WaitAsync(Limit, CancellationToken);

        Outcome(scheduled).ShouldBe(SyncPassFailureFactory.CreateAlreadyRunning());
        Outcome(first).ShouldBe("ran");
        Steps.Steps.Count(step => step == "index").ShouldBe(1);
        services.Logs.Logger.Messages.ShouldContain(message => message.Contains("Scheduled") && message.Contains("skipped"));
    }

    [Fact]
    public async Task when_a_manual_trigger_hits_a_running_tray_run_then_only_one_pass_runs()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var tray = RunAsync(Personal, SyncTriggerFactory.CreateTray());
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        var manual = await RunAsync(Personal, Manual).WaitAsync(Limit, CancellationToken);
        gate.Release();
        var first = await tray.WaitAsync(Limit, CancellationToken);

        Outcome(manual).ShouldBe(SyncPassFailureFactory.CreateAlreadyRunning());
        Outcome(first).ShouldBe("ran");
        Steps.Steps.Count(step => step == "download").ShouldBe(1);
    }

    [Fact]
    public async Task when_twenty_triggers_race_for_one_account_then_exactly_one_pass_runs()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);

        var racing = Enumerable.Range(0, 20).Select(_ => Task.Run(() => RunAsync(Personal, Manual), CancellationToken)).ToList();
        while (racing.Count(run => run.IsCompleted) < 19)
            await Task.Delay(5, CancellationToken);

        gate.Release();
        var results = await Task.WhenAll(racing).WaitAsync(Limit, CancellationToken);

        results.Count(result => Outcome(result) is "ran").ShouldBe(1);
        results.Count(result => Outcome(result) is SyncPassFailure.AlreadyRunning).ShouldBe(19);
        Steps.Steps.Count(step => step == "download").ShouldBe(1);
    }

    [Fact]
    public async Task when_two_accounts_sync_then_their_passes_run_at_the_same_time()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);

        var personal = RunAsync(Personal, Manual);
        var work = RunAsync(services.Work, Manual);
        await Steps.UntilDownloadsAsync(2, CancellationToken).WaitAsync(Limit, CancellationToken);
        gate.Release();
        var results = await Task.WhenAll(personal, work).WaitAsync(Limit, CancellationToken);

        Steps.MaxConcurrentDownloads.ShouldBe(2);
        results.Count(result => Outcome(result) is "ran").ShouldBe(2);
    }

    [Fact]
    public async Task when_the_lock_is_drained_then_no_run_starts()
    {
        using var drain = await services.SyncLock.DrainAsync(CancellationToken);

        var result = await RunAsync(Personal, Manual);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateAlreadyRunning());
        Steps.Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_running_pass_is_cancelled_then_it_reports_cancelled_stops_before_uploads_and_frees_the_account()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var running = RunAsync(Personal, Manual);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        await services.Cancel.CancelAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);

        running.IsCompleted.ShouldBeTrue();
        Outcome(await running).ShouldBe(SyncPassFailureFactory.CreateCancelled());
        Steps.Steps.ShouldNotContain("upload");
        services.Activity.ActivityOf(Personal).ShouldBe(AccountSyncActivityFactory.CreateIdle());
        services.SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    [Fact]
    public async Task when_the_callers_token_is_cancelled_then_the_run_reports_cancelled_and_does_not_throw()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var running = services.Runner.RunAsync(Personal, Manual, caller.Token);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        await caller.CancelAsync();
        var result = await running.WaitAsync(Limit, CancellationToken);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateCancelled());
    }

    [Fact]
    public async Task when_nothing_is_running_then_cancel_completes_at_once()
    {
        var cancelling = services.Cancel.CancelAsync(Personal, CancellationToken);

        await cancelling.WaitAsync(Limit, CancellationToken);

        cancelling.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_cancelled_run_has_ended_then_a_new_run_for_the_account_goes_ahead()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var running = RunAsync(Personal, Manual);
        await gate.Entered.WaitAsync(Limit, CancellationToken);
        await services.Cancel.CancelAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);
        Steps.DownloadGate = Option.None<WriteGate>();

        var next = await RunAsync(Personal, Manual).WaitAsync(Limit, CancellationToken);

        Outcome(await running).ShouldBe(SyncPassFailureFactory.CreateCancelled());
        Outcome(next).ShouldBe("ran");
    }

    [Fact]
    public async Task when_a_pass_is_running_then_the_activity_is_running_with_its_trigger_and_start_time()
    {
        var gate = new WriteGate();
        Steps.DownloadGate = Option.Some(gate);
        var running = RunAsync(Personal, Manual);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        var during = services.Activity.ActivityOf(Personal);
        var other = services.Activity.ActivityOf(services.Work);
        gate.Release();
        _ = await running.WaitAsync(Limit, CancellationToken);

        during.ShouldBe(AccountSyncActivityFactory.CreateRunning(Manual, services.Time.GetUtcNow()));
        other.ShouldBe(AccountSyncActivityFactory.CreateIdle());
        services.Activity.ActivityOf(Personal).ShouldBe(AccountSyncActivityFactory.CreateIdle());
    }

    [Fact]
    public async Task when_a_run_starts_and_ends_then_each_event_is_raised_once_with_the_account_trigger_and_outcome()
    {
        var started = new List<SyncPassStartedEventArgs>();
        var finished = new List<SyncPassFinishedEventArgs>();
        services.Activity.PassStarted += (_, args) => started.Add(args);
        services.Activity.PassFinished += (_, args) => finished.Add(args);
        Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        _ = await RunAsync(Personal, Manual);

        started.Select(args => (args.AccountId, args.Trigger, args.StartedAt)).ShouldBe([(Personal, Manual, services.Time.GetUtcNow())]);
        finished.Select(args => (args.AccountId, args.Trigger, Outcome(args.Outcome))).ShouldBe([(Personal, Manual, (object)SyncPassFailureFactory.CreateRootUnavailable())]);
    }

    [Fact]
    public async Task when_a_trigger_is_skipped_then_no_event_is_raised_for_it()
    {
        var started = 0;
        using var held = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(Personal)).Lease;
        services.Activity.PassStarted += (_, _) => started++;

        _ = await RunAsync(Personal, Manual);

        started.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_finished_event_is_raised_then_the_account_is_already_free()
    {
        var freeAtFinish = new List<bool>();
        services.Activity.PassFinished += (_, args) =>
        {
            var attempt = services.SyncLock.TryAcquire(args.AccountId);
            freeAtFinish.Add(attempt is SyncLockAttempt.Acquired);
            if (attempt is SyncLockAttempt.Acquired acquired)
                acquired.Lease.Dispose();
        };

        _ = await RunAsync(Personal, Manual);

        freeAtFinish.ShouldBe([true]);
    }

    public void Dispose() => services.Dispose();
}
