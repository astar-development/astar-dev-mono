using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPassForFirstSyncLinks : IDisposable
{
    private const string HelloQuickXorHash = "aCgDG9jwBgAAAAAABQAAAAAAAAA=";

    private readonly UploadPassFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private void WriteLocal(string content)
    {
        _ = fixture.FileSystem.Directory.CreateDirectory("/sync/Docs");
        fixture.FileSystem.File.WriteAllText("/sync/Docs/a.txt", content);
    }

    [Fact]
    public async Task when_both_sides_added_the_same_file_with_the_same_size_and_hash_then_it_is_linked_and_nothing_is_uploaded()
    {
        WriteLocal("hello");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5, HelloQuickXorHash);
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        var summary = result.Match(found => found, failure => throw new InvalidOperationException(failure.GetType().Name));
        (summary.Linked, summary.SentToConflicts, summary.Uploaded).ShouldBe((1, 0, 0));
        fixture.Uploader.Calls.ShouldBeEmpty();
        var row = fixture.Baseline.Find("Docs/a.txt");
        row.ItemId.ShouldBe(Id("r1"));
        row.Kind.ShouldBeOfType<SyncBaselineKind.File>().CTag.ShouldBe(new ContentTag("c1"));
    }

    [Fact]
    public async Task when_the_remote_file_has_no_hash_then_it_goes_to_conflicts_with_both_snapshots_and_nothing_is_linked()
    {
        WriteLocal("hello");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5);
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        var summary = result.Match(found => found, failure => throw new InvalidOperationException(failure.GetType().Name));
        var conflict = summary.Conflicts.ShouldHaveSingleItem();
        conflict.Kind.ShouldBe(ConflictKindFactory.CreateBothAdded());
        conflict.Local.ShouldBeOfType<SideSnapshot.Present>().Size.ShouldBe(5);
        conflict.Remote.ShouldBeOfType<SideSnapshot.Present>().Tag.ShouldBe(Option.Some(new ContentTag("c1")));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_hashes_differ_then_it_goes_to_conflicts_and_nothing_is_linked()
    {
        WriteLocal("world");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5, HelloQuickXorHash);
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(found => found.SentToConflicts, _ => -1).ShouldBe(1);
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_provider_answers_precondition_failed_then_the_conflict_carries_the_local_and_remote_snapshots()
    {
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 10);
        fixture.Uploader.Respond = _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreatePreconditionFailed());
        _ = await fixture.BaselineFile("Docs/a.txt", "r1", "c1").ModifiedFile("Docs/a.txt").BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        var conflict = result.Match(found => found.Conflicts, _ => []).ShouldHaveSingleItem();
        conflict.Local.ShouldBeOfType<SideSnapshot.Present>().Size.ShouldBe(20);
        conflict.Remote.ShouldBeOfType<SideSnapshot.Present>().Tag.ShouldBe(Option.Some(new ContentTag("c1")));
    }

    [Fact]
    public async Task when_the_local_file_cannot_be_read_then_the_item_fails_and_no_conflict_is_recorded()
    {
        WriteLocal("hello");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5, HelloQuickXorHash);
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);
        using var handle = fixture.FileSystem.File.Open("/sync/Docs/a.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = await fixture.RunAsync(CancellationToken);

        var summary = result.Match(found => found, failure => throw new InvalidOperationException(failure.GetType().Name));
        (summary.Failed, summary.SentToConflicts, summary.Linked).ShouldBe((1, 0, 0));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_local_file_changed_size_since_the_scan_then_the_item_is_deferred_and_not_a_conflict()
    {
        WriteLocal("hello!");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5, HelloQuickXorHash);
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        var summary = result.Match(found => found, failure => throw new InvalidOperationException(failure.GetType().Name));
        (summary.Deferred, summary.SentToConflicts, summary.Linked).ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task when_both_sides_added_an_empty_file_then_it_is_linked()
    {
        WriteLocal(string.Empty);
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 0, "AAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        _ = await fixture.AddedFile("Docs/a.txt", 0).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(found => (found.Linked, found.SentToConflicts), _ => (-1, -1)).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_the_run_is_cancelled_while_a_file_is_being_linked_then_the_cancel_propagates_and_nothing_is_linked()
    {
        _ = fixture.FileSystem.Directory.CreateDirectory("/sync/Docs");
        fixture.FileSystem.File.WriteAllBytes("/sync/Docs/a.txt", [.. Enumerable.Range(0, 200000).Select(index => (byte)((index * 31 + 7) % 251))]);
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 200000, "0lppLlE1sn/syuQ/kvKFqzJQT90=");
        _ = await fixture.AddedFile("Docs/a.txt", 200000).BuildAsync(CancellationToken);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Feed.ProgressChanged += (_, _) => cancel.Cancel();

        var run = fixture.RunAsync(cancel.Token);

        await Should.ThrowAsync<OperationCanceledException>(() => run);
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_file_is_linked_then_the_progress_reports_its_bytes_against_a_total_that_includes_it()
    {
        WriteLocal("hello");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5, HelloQuickXorHash);
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);
        List<SyncProgress> seen = [];
        fixture.Feed.ProgressChanged += (_, args) => seen.Add(args.Progress);

        _ = await fixture.RunAsync(CancellationToken);

        seen.ShouldContain(progress => progress.BytesDone == 5 && progress.BytesTotal == 5);
    }

    [Fact]
    public async Task when_the_baseline_cannot_be_written_for_a_link_then_the_item_fails_and_is_not_stored()
    {
        WriteLocal("hello");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("Docs/a.txt", "r1", "c1", 5, HelloQuickXorHash);
        fixture.Baseline.UpsertFails = true;
        _ = await fixture.AddedFile("Docs/a.txt", 5).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(found => (found.Failed, found.Linked), _ => (-1, -1)).ShouldBe((1, 0));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_cloud_path_differs_in_case_from_the_local_path_then_the_file_is_linked_under_the_local_path()
    {
        _ = fixture.FileSystem.Directory.CreateDirectory("/sync/Docs");
        fixture.FileSystem.File.WriteAllText("/sync/Docs/A.txt", "hello");
        fixture.Remote.Folder("Docs", "remote-docs");
        fixture.Remote.File("docs/a.TXT", "r1", "c1", 5, HelloQuickXorHash);
        _ = await fixture.AddedFile("Docs/A.txt", 5).BuildAsync(CancellationToken);

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(found => (found.Linked, found.SentToConflicts), _ => (-1, -1)).ShouldBe((1, 0));
        fixture.Baseline.Find("docs/a.txt").Path.Value.ShouldBe("Docs/A.txt");
    }

    [Fact]
    public async Task when_twenty_files_both_sides_added_are_linked_in_one_pass_then_every_one_is_linked_and_stored_once()
    {
        fixture.MaxConcurrentUploads = 4;
        fixture.Remote.Folder("Docs", "remote-docs");
        _ = fixture.FileSystem.Directory.CreateDirectory("/sync/Docs");
        foreach (var index in Enumerable.Range(0, 20))
        {
            fixture.FileSystem.File.WriteAllText($"/sync/Docs/f{index}.txt", "hello");
            fixture.Remote.File($"Docs/f{index}.txt", $"r{index}", "c1", 5, HelloQuickXorHash);
            _ = fixture.AddedFile($"Docs/f{index}.txt", 5);
        }

        _ = await fixture.BuildAsync(CancellationToken);

        var result = await Task.Run(() => fixture.RunAsync(CancellationToken), CancellationToken);

        result.Match(found => (found.Linked, found.SentToConflicts, found.Failed), _ => (-1, -1, -1)).ShouldBe((20, 0, 0));
        fixture.Baseline.All.Count(entry => entry.Kind is SyncBaselineKind.File).ShouldBe(20);
    }

    public void Dispose() => fixture.Dispose();
}
