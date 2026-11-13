using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALocalChangeDetector : IDisposable
{
    private readonly LocalScanFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture.Dispose();

    [Fact]
    public async Task when_a_file_is_not_in_the_baseline_then_it_is_added()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/new.txt", "hello");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/new.txt:File");
    }

    [Fact]
    public async Task when_a_new_file_is_added_then_its_size_and_time_are_reported_and_it_is_not_hashed()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/new.txt", "hello", TimeSpan.FromMinutes(5));

        var set = await fixture.ScanAsync(CancellationToken);

        var added = set.Changes.Single().ShouldBeOfType<LocalChange.Added>();
        (added.Size, added.LastWriteUtc, fixture.Hasher.Calls).ShouldBe((5, new DateTimeOffset(2026, 11, 13, 11, 55, 0, TimeSpan.Zero), 0));
    }

    [Fact]
    public async Task when_a_folder_is_not_in_the_baseline_then_it_is_added_as_a_folder()
    {
        fixture.Include("Docs");
        fixture.Folder("Docs/Sub");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/Sub:Folder");
    }

    [Fact]
    public async Task when_nothing_changed_then_nothing_is_reported_and_nothing_is_hashed()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "same", "item-a");
        _ = fixture.SyncedFolder("Docs/Sub", "item-sub");

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Changes.Count, fixture.Hasher.Calls, fixture.Baseline.Upserts).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task when_a_file_changed_then_it_is_hashed_once_and_reported_as_modified()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "before", "item-a", TimeSpan.FromMinutes(30));
        _ = fixture.Synced("Docs/b.txt", "stays", "item-b");
        _ = fixture.Synced("Docs/c.txt", "stays too", "item-c");
        _ = fixture.Write("Docs/a.txt", "after!", TimeSpan.FromMinutes(5));

        var set = await fixture.ScanAsync(CancellationToken);

        (LocalScanFixture.Describe(set), fixture.Hasher.Calls, set.Summary.Hashed).ShouldBe(("Modified:Docs/a.txt", 1, 1));
    }

    [Fact]
    public async Task when_only_the_write_time_changed_then_nothing_is_reported_and_the_baseline_time_is_refreshed()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "content", "item-a", TimeSpan.FromMinutes(30));
        _ = fixture.Write("Docs/a.txt", "content", TimeSpan.FromMinutes(5));

        var set = await fixture.ScanAsync(CancellationToken);

        var refreshed = ((SyncBaselineKind.File)fixture.Baseline.Find("Docs/a.txt").Kind).LastWriteUtc;
        (set.Changes.Count, set.Summary.Touched, refreshed).ShouldBe((0, 1, new DateTimeOffset(2026, 11, 13, 11, 55, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task when_the_size_changed_but_the_content_hash_is_the_same_then_it_is_not_reported()
    {
        fixture.Include("Docs");
        var entry = fixture.Synced("Docs/a.txt", "content", "item-a");
        var file = (SyncBaselineKind.File)entry.Kind;
        fixture.Baseline.Add(SyncBaselineEntryFactory.Create(LocalScanFixture.Account, entry.Path, entry.ItemId, SyncBaselineKindFactory.CreateFile(file.Size + 100, file.LastWriteUtc, file.Hash, file.CTag, file.ETag)));

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Changes.Count, set.Summary.Touched).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_a_baseline_file_is_gone_then_it_is_deleted_with_its_remote_id()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "x", "item-a");
        fixture.FileSystem.File.Delete($"{LocalScanFixture.RootPath}/Docs/a.txt");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Deleted:Docs/a.txt:item-a");
    }

    [Fact]
    public async Task when_a_baseline_folder_is_gone_then_it_is_deleted()
    {
        fixture.Include("Docs");
        _ = fixture.SyncedFolder("Docs/Sub", "item-sub");
        fixture.FileSystem.Directory.Delete($"{LocalScanFixture.RootPath}/Docs/Sub");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Deleted:Docs/Sub:item-sub");
    }

    [Fact]
    public async Task when_only_deletions_are_found_then_the_summary_still_counts_them()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "x", "item-a");
        fixture.FileSystem.File.Delete($"{LocalScanFixture.RootPath}/Docs/a.txt");

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Summary.Deleted, set.Summary.Added, set.Summary.Modified).ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task when_a_new_file_changed_in_the_last_two_seconds_then_it_is_deferred_not_added()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/busy.txt", "half", TimeSpan.FromSeconds(1));

        var set = await fixture.ScanAsync(CancellationToken);

        (LocalScanFixture.Describe(set), set.Summary.Deferred).ShouldBe(("Deferred:Docs/busy.txt", 1));
    }

    [Fact]
    public async Task when_a_baseline_file_was_just_modified_then_it_is_deferred_and_not_hashed()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "before", "item-a", TimeSpan.FromMinutes(30));
        _ = fixture.Write("Docs/a.txt", "after!", TimeSpan.FromSeconds(1));

        var set = await fixture.ScanAsync(CancellationToken);

        (LocalScanFixture.Describe(set), fixture.Hasher.Calls).ShouldBe(("Deferred:Docs/a.txt", 0));
    }

    [Fact]
    public async Task when_the_clock_moves_past_two_seconds_then_the_deferred_file_is_added()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/busy.txt", "done", TimeSpan.FromSeconds(1));
        _ = await fixture.ScanAsync(CancellationToken);
        fixture.Time.Advance(TimeSpan.FromSeconds(2));

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/busy.txt:File");
    }

    [Fact]
    public async Task when_a_recent_file_is_unchanged_from_the_baseline_then_it_is_not_deferred()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "same", "item-a", TimeSpan.FromSeconds(1));

        var set = await fixture.ScanAsync(CancellationToken);

        set.Changes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_root_folder_is_missing_then_the_scan_fails_as_root_unavailable_with_no_changes()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "x", "item-a");
        fixture.FileSystem.Directory.Delete(LocalScanFixture.RootPath, recursive: true);

        var result = await fixture.Detector.DetectAsync(LocalScanFixture.Account, CancellationToken);

        result.Match(_ => "scanned", failure => failure.GetType().Name).ShouldBe(nameof(LocalScanFailure.RootUnavailable));
    }

    [Fact]
    public async Task when_the_account_has_no_sync_root_then_the_scan_fails_as_root_unavailable()
    {
        fixture.Roots.Root = Option.None<System.IO.Abstractions.IDirectoryInfo>();

        var result = await fixture.Detector.DetectAsync(LocalScanFixture.Account, CancellationToken);

        result.Match(_ => "scanned", failure => failure.GetType().Name).ShouldBe(nameof(LocalScanFailure.RootUnavailable));
    }

    [Fact]
    public async Task when_the_sync_root_cannot_be_read_then_the_scan_fails_as_unexpected()
    {
        fixture.Roots.Failure = Result.Failure<Option<SyncRoot>, ErrorResponse>(new ErrorResponse("locked"));

        var result = await fixture.Detector.DetectAsync(LocalScanFixture.Account, CancellationToken);

        result.Match(_ => "scanned", failure => failure.GetType().Name).ShouldBe(nameof(LocalScanFailure.Unexpected));
    }

    [Fact]
    public async Task when_the_rules_cannot_be_read_then_the_scan_fails_as_unexpected()
    {
        fixture.Rules.Listing = Result.Failure<IReadOnlyList<SelectionRule>, ErrorResponse>(new ErrorResponse("locked"));

        var result = await fixture.Detector.DetectAsync(LocalScanFixture.Account, CancellationToken);

        result.Match(_ => "scanned", failure => failure.GetType().Name).ShouldBe(nameof(LocalScanFailure.Unexpected));
    }

    [Fact]
    public async Task when_no_rule_includes_anything_then_nothing_is_scanned_and_nothing_is_deleted()
    {
        _ = fixture.Write("Docs/a.txt", "x");
        _ = fixture.Synced("Docs/b.txt", "y", "item-b");
        fixture.FileSystem.File.Delete($"{LocalScanFixture.RootPath}/Docs/b.txt");

        var set = await fixture.ScanAsync(CancellationToken);

        set.Changes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task when_two_rules_overlap_with_a_casing_variant_then_each_file_is_reported_once()
    {
        fixture.Include("Docs", "docs/work", "DOCS");
        _ = fixture.Write("Docs/Work/plan.txt", "plan");
        _ = fixture.Write("Docs/top.txt", "top");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/top.txt:File;Added:Docs/Work:Folder;Added:Docs/Work/plan.txt:File");
    }

    [Fact]
    public async Task when_a_rule_is_deeper_than_a_non_included_parent_then_only_the_included_folder_is_scanned()
    {
        fixture.Include("Docs/Work");
        _ = fixture.Write("Docs/Work/plan.txt", "plan");
        _ = fixture.Write("Docs/skipped.txt", "no");
        _ = fixture.Write("Other/skipped.txt", "no");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/Work/plan.txt:File");
    }

    [Fact]
    public async Task when_a_subfolder_is_excluded_then_it_is_not_scanned_and_its_baseline_items_are_not_deleted()
    {
        fixture.SetRules(SelectionTestRules.Include("Docs"), SelectionTestRules.Exclude("Docs/Private"));
        _ = fixture.Write("Docs/Private/secret.txt", "s");
        _ = fixture.Synced("Docs/Private/old.txt", "o", "item-old");
        fixture.FileSystem.File.Delete($"{LocalScanFixture.RootPath}/Docs/Private/old.txt");
        _ = fixture.Write("Docs/open.txt", "o");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/open.txt:File");
    }

    [Fact]
    public async Task when_a_dot_prefixed_folder_is_found_then_it_is_not_scanned_but_dot_files_are()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/.git/config", "c");
        _ = fixture.Write("Docs/.gitignore", "bin");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/.gitignore:File");
    }

    [Fact]
    public async Task when_temp_lock_and_system_files_are_found_then_they_are_not_reported()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/Report.docx.3f2a9c1e-5b7d-4e6a-8c0d-1a2b3c4d5e6f.download", "t");
        _ = fixture.Write("Docs/~$Report.docx", "lock");
        _ = fixture.Write("Docs/Thumbs.db", "db");
        _ = fixture.Write("Docs/Report.docx", "real");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/Report.docx:File");
    }

    [Fact]
    public async Task when_a_file_cannot_be_hashed_then_it_is_unreadable_and_the_scan_goes_on()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/locked.txt", "before", "item-locked", TimeSpan.FromMinutes(30));
        _ = fixture.Synced("Docs/fine.txt", "before", "item-fine", TimeSpan.FromMinutes(30));
        _ = fixture.Write("Docs/locked.txt", "after1", TimeSpan.FromMinutes(5));
        _ = fixture.Write("Docs/fine.txt", "after2", TimeSpan.FromMinutes(5));
        _ = fixture.Hasher.UnreadableNames.Add("locked.txt");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Modified:Docs/fine.txt;Unreadable:Docs/locked.txt");
    }

    [Fact]
    public async Task when_two_siblings_differ_only_in_case_then_the_ambiguous_path_is_unreadable()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/Report.txt", "one");
        _ = fixture.Write("Docs/report.TXT", "two");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Unreadable:Docs/report.TXT");
    }

    [Fact]
    public async Task when_the_baseline_is_large_then_it_is_read_in_pages_of_at_most_5000()
    {
        fixture.Include("Docs");
        foreach (var index in Enumerable.Range(0, 12000))
            fixture.Baseline.Add(SyncBaselineEntryFactory.Create(LocalScanFixture.Account, LocalScanFixture.Path($"Docs/file{index:00000}.txt"), RemoteIds.Of($"item-{index}"), SyncBaselineKindFactory.CreateFolder()));

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Summary.Deleted, fixture.Baseline.LargestPageReturned <= 5000, fixture.Baseline.Pages >= 3).ShouldBe((12000, true, true));
    }

    [Fact]
    public async Task when_the_baseline_casing_differs_from_the_disk_casing_then_the_file_is_not_reported_deleted()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("docs/a.txt", "same", "item-a");
        fixture.Baseline.Add(SyncBaselineEntryFactory.Create(LocalScanFixture.Account, LocalScanFixture.Path("Docs"), RemoteIds.Of("item-docs"), SyncBaselineKindFactory.CreateFolder()));
        var entry = fixture.Baseline.Find("docs/a.txt");
        fixture.Baseline.Add(SyncBaselineEntryFactory.Create(LocalScanFixture.Account, LocalScanFixture.Path("Docs/A.TXT"), entry.ItemId, entry.Kind));

        var set = await fixture.ScanAsync(CancellationToken);

        set.Changes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_baseline_file_became_a_folder_then_the_path_is_unreadable_and_nothing_is_transferred()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/thing", "was a file", "item-thing");
        fixture.FileSystem.File.Delete($"{LocalScanFixture.RootPath}/Docs/thing");
        fixture.Folder("Docs/thing");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Unreadable:Docs/thing");
    }

    [Fact]
    public async Task when_a_baseline_folder_became_a_file_then_the_path_is_unreadable_and_nothing_is_transferred()
    {
        fixture.Include("Docs");
        _ = fixture.SyncedFolder("Docs/thing", "item-thing");
        fixture.FileSystem.Directory.Delete($"{LocalScanFixture.RootPath}/Docs/thing");
        _ = fixture.Write("Docs/thing", "now a file");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Unreadable:Docs/thing");
    }

    [Fact]
    public async Task when_a_new_file_has_a_write_time_a_second_in_the_future_then_it_is_deferred()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/skewed.txt", "x", TimeSpan.FromSeconds(-1));

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Deferred:Docs/skewed.txt");
    }

    [Fact]
    public async Task when_the_baseline_refresh_fails_then_the_real_changes_are_still_reported()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/touched.txt", "content", "item-t", TimeSpan.FromMinutes(30));
        _ = fixture.Write("Docs/touched.txt", "content", TimeSpan.FromMinutes(5));
        _ = fixture.Write("Docs/new.txt", "new");
        fixture.Baseline.UpsertFails = true;

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/new.txt:File");
    }

    [Fact]
    public async Task when_the_scan_is_cancelled_while_hashing_then_it_throws_and_the_baseline_is_not_touched()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "before", "item-a", TimeSpan.FromMinutes(30));
        _ = fixture.Write("Docs/a.txt", "after!", TimeSpan.FromMinutes(5));
        using var source = new CancellationTokenSource();
        fixture.Hasher.CancelOnHash = source;

        await Should.ThrowAsync<OperationCanceledException>(() => fixture.Detector.DetectAsync(LocalScanFixture.Account, source.Token));

        fixture.Baseline.Upserts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_the_scan_throws_and_reports_nothing()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/a.txt", "x");
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => fixture.Detector.DetectAsync(LocalScanFixture.Account, source.Token));

        fixture.Baseline.Upserts.ShouldBe(0);
    }

    [Fact]
    public async Task when_many_scans_run_at_once_then_each_reports_the_same_changes()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/a.txt", "a");
        _ = fixture.Write("Docs/b.txt", "b");

        var scans = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => fixture.ScanAsync(CancellationToken), CancellationToken)));

        scans.Select(LocalScanFixture.Describe).Distinct().ShouldBe(["Added:Docs/a.txt:File;Added:Docs/b.txt:File"]);
    }

    [Fact]
    public async Task when_a_scan_logs_then_no_path_or_file_name_is_logged_and_the_scope_carries_the_hashed_account()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/Tax return 2025.pdf", "x");

        _ = await fixture.ScanAsync(CancellationToken);

        (fixture.Logs.Logger.Messages.Count > 0, fixture.Logs.Logger.Messages.All(message => !message.Contains("Tax", StringComparison.Ordinal) && !message.Contains("Docs", StringComparison.Ordinal)), fixture.Logs.Logger.Scopes.Any(scope => scope.ContainsKey(SyncLogScope.AccountIdHashKey))).ShouldBe((true, true, true));
    }
}
