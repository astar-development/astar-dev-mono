using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncRunStatusSource : IDisposable
{
    private static readonly SyncTrigger Manual = SyncTriggerFactory.CreateManual();
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AccountId Personal => services.Personal;

    private Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(AccountId accountId) => services.Runner.RunAsync(accountId, Manual, CancellationToken);

    [Fact]
    public void when_no_run_has_ended_then_the_account_is_up_to_date_and_never_synced()
    {
        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
        services.Status.LastSyncOf(Personal).ShouldBe(Option.None<DateTimeOffset>());
    }

    [Fact]
    public async Task when_a_run_is_in_progress_then_the_account_is_syncing()
    {
        var gate = new WriteGate();
        services.Steps.DownloadGate = Option.Some(gate);
        var running = RunAsync(Personal);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        var status = services.Status.StatusOf(Personal);
        gate.Release();
        await running.WaitAsync(Limit, CancellationToken);

        status.ShouldBe(SyncStatusFactory.CreateSyncing(0));
    }

    [Fact]
    public async Task when_a_run_finishes_with_nothing_waiting_then_the_account_is_up_to_date_and_the_finish_time_is_kept()
    {
        services.Time.Advance(TimeSpan.FromMinutes(5));

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
        services.Status.LastSyncOf(Personal).ShouldBe(Option.Some(services.Time.GetUtcNow()));
    }

    [Fact]
    public async Task when_a_run_finishes_with_conflicts_and_held_deletions_then_the_account_needs_attention()
    {
        var path = RelativeSyncPathFactory.Create("Docs/a.txt").Match(found => found, _ => throw new InvalidOperationException("The test path must be valid."));
        services.Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictKindFactory.CreateBothModified()), DownloadItemOutcomeFactory.CreateHeldByGuard()], []));
        services.Steps.Uploaded = Result.Success<UploadPassSummary, UploadPassFailure>(UploadPassSummaryFactory.Create([UploadItemOutcomeFactory.CreateSentToConflicts(path, ConflictKindFactory.CreateBothAdded())]));

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateNeedsAttention(2, 1));
        services.Status.LastSyncOf(Personal).Match(_ => true, () => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_run_ends_with_only_held_deletions_then_the_account_does_not_show_as_up_to_date()
    {
        services.Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateHeldByGuard()], []));

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateNeedsAttention(0, 1));
    }

    [Fact]
    public async Task when_a_run_fails_then_the_account_shows_the_reason_and_the_finish_time_is_not_kept()
    {
        services.Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateRootUnavailable()));
        services.Status.LastSyncOf(Personal).ShouldBe(Option.None<DateTimeOffset>());
    }

    [Fact]
    public async Task when_the_upload_pass_reports_a_full_provider_then_the_account_shows_quota_exceeded()
    {
        services.Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateQuotaExceeded());

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()));
    }

    [Fact]
    public async Task when_the_download_pass_reports_a_full_disk_then_the_account_shows_insufficient_disk_space()
    {
        services.Steps.Downloaded = Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateInsufficientDiskSpace());

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateInsufficientDiskSpace()));
    }

    [Fact]
    public async Task when_a_step_fails_unexpectedly_then_the_account_shows_an_unexpected_failure()
    {
        services.Steps.Index = Result.Failure<RemoteUpdateSummary, ProviderError>(ProviderErrorFactory.CreateTransient());

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()));
    }

    [Fact]
    public async Task when_a_run_is_cancelled_then_the_account_shows_cancelled_not_failed()
    {
        var gate = new WriteGate();
        services.Steps.DownloadGate = Option.Some(gate);
        var running = RunAsync(Personal);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        await services.Cancel.CancelAsync(Personal, CancellationToken);
        await running.WaitAsync(Limit, CancellationToken);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateCancelled());
        services.Status.LastSyncOf(Personal).ShouldBe(Option.None<DateTimeOffset>());
    }

    [Fact]
    public async Task when_a_run_needs_sign_in_then_the_status_is_left_as_it_was()
    {
        await RunAsync(Personal);
        services.Steps.Index = Result.Failure<RemoteUpdateSummary, ProviderError>(ProviderErrorFactory.CreateAuthenticationRequired());

        await RunAsync(Personal);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_a_trigger_is_skipped_because_a_run_is_in_progress_then_the_last_status_is_not_replaced()
    {
        services.Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());
        await RunAsync(Personal);
        var gate = new WriteGate();
        services.Steps.Scan = Result.Success<LocalChangeSet, LocalScanFailure>(LocalChangeSetFactory.Create([], 0, 0));
        services.Steps.DownloadGate = Option.Some(gate);
        var running = RunAsync(Personal);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        await RunAsync(Personal);
        gate.Release();
        await running.WaitAsync(Limit, CancellationToken);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_two_accounts_run_then_each_keeps_its_own_status()
    {
        services.Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());
        await RunAsync(Personal);
        services.Steps.Scan = Result.Success<LocalChangeSet, LocalScanFailure>(LocalChangeSetFactory.Create([], 0, 0));
        await RunAsync(services.Work);

        services.Status.StatusOf(Personal).ShouldBeOfType<SyncStatus.Failed>();
        services.Status.StatusOf(services.Work).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_twenty_runs_race_for_one_account_then_the_status_is_one_of_the_outcomes()
    {
        var racing = Enumerable.Range(0, 20).Select(_ => Task.Run(() => RunAsync(Personal), CancellationToken));

        await Task.WhenAll(racing);

        services.Status.StatusOf(Personal).ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public void when_a_needs_attention_status_is_created_with_negative_counts_then_they_are_zero()
    {
        var status = SyncStatusFactory.CreateNeedsAttention(-3, -1);

        status.ShouldBeOfType<SyncStatus.NeedsAttention>().Conflicts.ShouldBe(0);
        status.ShouldBeOfType<SyncStatus.NeedsAttention>().HeldDeletions.ShouldBe(0);
    }

    public void Dispose() => services.Dispose();
}
