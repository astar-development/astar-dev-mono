using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncPassRunnerThatWritesAttentionItems : IDisposable
{
    private static readonly SyncTrigger Manual = SyncTriggerFactory.CreateManual();
    private static readonly DateTimeOffset Moment = new(2026, 11, 14, 8, 0, 0, TimeSpan.Zero);

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ScriptedSyncSteps Steps => services.Steps;

    private AccountId Personal => services.Personal;

    private static RelativeSyncPath At(string value) => new(value);

    private static object Outcome(Result<SyncPassSummary, SyncPassFailure> result) => result.Match(_ => (object)"ran", failure => failure);

    private Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync() => services.Runner.RunAsync(Personal, Manual, CancellationToken);

    private SyncAttentionItem.Conflict Seed(string path, ConflictKind kind, SideSnapshot local)
    {
        var item = (SyncAttentionItem.Conflict)SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), Personal, At(path), kind, local, ConflictFindings.Present(), Moment);
        services.Attention.Seed(item);

        return item;
    }

    private void Baselined(string path, string itemId, string cTag) =>
        services.Baseline.Add(SyncBaselineEntryFactory.Create(Personal, At(path), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFile(10, Moment, RemoteIds.Hash("h"), RemoteIds.CTag(cTag), RemoteIds.ETag("e-" + cTag))));

    private void ScanFinds(params LocalChange[] changes) => Steps.Scan = Result.Success<LocalChangeSet, LocalScanFailure>(LocalChangeSetFactory.Create(changes, changes.Length, 0));

    private static LocalChange Modified(string path) => LocalChangeFactory.CreateModified(At(path), 5, Moment, RemoteIds.Hash("local"));

    [Fact]
    public async Task when_the_passes_find_conflicts_then_the_runner_records_one_attention_item_each_with_both_snapshots()
    {
        var download = ConflictFindingFactory.Create(At("Docs/a.txt"), ConflictKindFactory.CreateBothModified(), ConflictFindings.Present(), SideSnapshotFactory.CreateAbsent());
        var upload = ConflictFindings.Of("Docs/b.txt", ConflictKindFactory.CreateBothAdded());
        Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateSentToConflicts(download)], []));
        Steps.Uploaded = Result.Success<UploadPassSummary, UploadPassFailure>(UploadPassSummaryFactory.Create([UploadItemOutcomeFactory.CreateSentToConflicts(upload)]));

        var result = await RunAsync();

        Outcome(result).ShouldBe("ran");
        var recorded = services.Attention.Conflicts.OrderBy(conflict => conflict.Path.Value).ToList();
        recorded.Select(conflict => (conflict.Path.Value, conflict.Kind.GetType().Name, conflict.AccountId, conflict.DetectedAt)).ShouldBe([("Docs/a.txt", "BothModified", Personal, services.Time.GetUtcNow()), ("Docs/b.txt", "BothAdded", Personal, services.Time.GetUtcNow())]);
        recorded[0].Remote.ShouldBeOfType<SideSnapshot.Absent>();
        recorded[0].Local.ShouldBeOfType<SideSnapshot.Present>();
    }

    [Fact]
    public async Task when_the_download_pass_holds_deletions_then_the_runner_records_them_as_held_local_deletions()
    {
        var pair = SyncPairIdFactory.Create(Guid.NewGuid());
        var held = HeldLocalDeletionsFactory.Create(pair, [At("Docs/a.txt"), At("Docs/b.txt")], 100);
        Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([], [held]));

        _ = await RunAsync();

        var recorded = services.Attention.Held.ShouldHaveSingleItem();
        recorded.Direction.ShouldBeOfType<DeletionDirection.DeleteLocal>();
        recorded.PairId.ShouldBe(pair);
        recorded.Paths.Select(path => path.Value).ShouldBe(["Docs/a.txt", "Docs/b.txt"]);
    }

    [Fact]
    public async Task when_the_passes_find_nothing_then_the_runner_writes_nothing()
    {
        _ = await RunAsync();

        services.Attention.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_attention_write_fails_then_the_run_is_unexpected()
    {
        Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictFindings.Of("Docs/a.txt", ConflictKindFactory.CreateBothModified()))], []));
        services.Attention.Fails = true;

        var result = await RunAsync();

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
    }

    [Fact]
    public async Task when_the_upload_pass_fails_then_the_conflicts_the_download_pass_found_are_still_recorded()
    {
        Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictFindings.Of("Docs/a.txt", ConflictKindFactory.CreateBothModified()))], []));
        Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateQuotaExceeded());

        var result = await RunAsync();

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateQuotaExceeded());
        services.Attention.Conflicts.ShouldHaveSingleItem().Path.Value.ShouldBe("Docs/a.txt");
    }

    [Fact]
    public async Task when_the_cloud_side_returned_to_the_baseline_then_the_conflict_is_closed_as_auto_cleared()
    {
        var conflict = Seed("Docs/a.txt", ConflictKindFactory.CreateBothModified(), ConflictFindings.Present());
        Baselined("Docs/a.txt", "a", "c1");
        services.Remote.File("Docs/a.txt", "a", "c1", 10);
        ScanFinds(Modified("Docs/a.txt"));

        var result = await RunAsync();

        Outcome(result).ShouldBe("ran");
        services.Attention.ClosedConflicts.ShouldHaveSingleItem().ShouldBe((conflict.Id, AttentionResolutionFactory.CreateAutoCleared()));
    }

    [Fact]
    public async Task when_the_local_side_returned_to_the_baseline_then_the_conflict_is_closed_as_auto_cleared()
    {
        var conflict = Seed("Docs/a.txt", ConflictKindFactory.CreateBothModified(), ConflictFindings.Present());
        Baselined("Docs/a.txt", "a", "c1");
        services.Remote.File("Docs/a.txt", "a", "c2", 10);
        ScanFinds();

        _ = await RunAsync();

        services.Attention.ClosedConflicts.Select(closed => closed.Id).ShouldBe([conflict.Id]);
    }

    [Fact]
    public async Task when_both_sides_still_differ_from_the_baseline_then_the_conflict_stays_open()
    {
        _ = Seed("Docs/a.txt", ConflictKindFactory.CreateBothModified(), ConflictFindings.Present());
        Baselined("Docs/a.txt", "a", "c1");
        services.Remote.File("Docs/a.txt", "a", "c2", 10);
        ScanFinds(Modified("Docs/a.txt"));

        _ = await RunAsync();

        services.Attention.ClosedConflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_path_has_no_baseline_then_the_conflict_stays_open()
    {
        _ = Seed("Docs/a.txt", ConflictKindFactory.CreateBothAdded(), ConflictFindings.Present());
        services.Remote.File("Docs/a.txt", "a", "c1", 10);
        ScanFinds();

        _ = await RunAsync();

        services.Attention.ClosedConflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_local_side_was_absent_and_the_scan_shows_no_change_then_the_conflict_stays_open()
    {
        _ = Seed("Docs/a.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), SideSnapshotFactory.CreateAbsent());
        Baselined("Docs/a.txt", "a", "c1");
        services.Remote.File("Docs/a.txt", "a", "c2", 10);
        ScanFinds();

        _ = await RunAsync();

        services.Attention.ClosedConflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_cloud_item_was_deleted_and_the_local_file_still_differs_then_the_conflict_stays_open()
    {
        _ = Seed("Docs/a.txt", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), ConflictFindings.Present());
        Baselined("Docs/a.txt", "a", "c1");
        ScanFinds(Modified("Docs/a.txt"));

        _ = await RunAsync();

        services.Attention.ClosedConflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_conflict_is_auto_cleared_then_it_is_closed_before_the_download_pass_plans()
    {
        _ = Seed("Docs/a.txt", ConflictKindFactory.CreateBothModified(), ConflictFindings.Present());
        Baselined("Docs/a.txt", "a", "c1");
        services.Remote.File("Docs/a.txt", "a", "c1", 10);
        ScanFinds(Modified("Docs/a.txt"));
        var closedWhenDownloadStarted = -1;
        Steps.OnDownload = () => closedWhenDownloadStarted = services.Attention.ClosedConflicts.Count;

        _ = await RunAsync();

        closedWhenDownloadStarted.ShouldBe(1);
    }

    [Fact]
    public async Task when_closing_a_conflict_fails_then_the_run_is_unexpected_and_no_pass_runs()
    {
        _ = Seed("Docs/a.txt", ConflictKindFactory.CreateBothModified(), ConflictFindings.Present());
        Baselined("Docs/a.txt", "a", "c1");
        services.Remote.File("Docs/a.txt", "a", "c1", 10);
        services.Attention.CloseFails = true;

        var result = await RunAsync();

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
        Steps.Steps.ShouldBe(["index", "scan"]);
    }

    [Fact]
    public async Task when_the_attention_queue_cannot_be_read_then_the_run_is_unexpected_and_no_pass_runs()
    {
        services.Attention.ReadFails = true;

        var result = await RunAsync();

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateUnexpected());
        Steps.Steps.ShouldBe(["index", "scan"]);
    }

    public void Dispose() => services.Dispose();
}
