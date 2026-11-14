using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPassForFirstSyncLinks
{
    private const string HelloQuickXorHash = "aCgDG9jwBgAAAAAABQAAAAAAAAA=";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static LocalChange AddedFile(string path, long size) => LocalChangeFactory.CreateAdded(Path(path), LocalItemKindFactory.CreateFile(), size, new DateTimeOffset(2026, 11, 13, 11, 50, 0, TimeSpan.Zero));

    [Fact]
    public async Task when_both_sides_added_the_same_file_with_the_same_size_and_hash_then_it_is_linked_in_the_baseline_and_not_downloaded()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        summary.LinkedExisting.ShouldBe(1);
        summary.SentToConflicts.ShouldBe(0);
        summary.Downloaded.ShouldBe(0);
        fixture.Downloader.Calls.ShouldBeEmpty();
        fixture.FileSystem.File.ReadAllText("/sync/root/Docs/a.txt").ShouldBe("hello");
        var row = fixture.Baseline.Find("Docs/a.txt");
        row.ItemId.ShouldBe(RemoteIds.Of("a"));
        var file = row.Kind.ShouldBeOfType<SyncBaselineKind.File>();
        file.Size.ShouldBe(5);
        file.CTag.ShouldBe(RemoteIds.CTag("c1"));
    }

    [Fact]
    public async Task when_a_longer_binary_file_matches_the_cloud_hash_then_it_is_linked()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.bin", string.Empty);
        fixture.FileSystem.File.WriteAllBytes("/sync/root/Docs/a.bin", [.. Enumerable.Range(0, 768).Select(index => (byte)(index % 256))]);
        fixture.Remote.File("Docs/a.bin", "a", "c1", 768, "rxAOGe1RimTF/e+k/m0O5nnSZT8=");

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.bin", 768)), CancellationToken));

        summary.LinkedExisting.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_remote_file_has_no_hash_then_it_is_not_linked_and_goes_to_conflicts_as_both_added()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        summary.LinkedExisting.ShouldBe(0);
        var conflict = summary.Conflicts.ShouldHaveSingleItem();
        conflict.Kind.ShouldBe(ConflictKindFactory.CreateBothAdded());
        conflict.Path.ShouldBe(Path("Docs/a.txt"));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_sizes_match_but_the_hashes_differ_then_it_goes_to_conflicts_with_both_snapshots()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "world");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        var conflict = summary.Conflicts.ShouldHaveSingleItem();
        var local = conflict.Local.ShouldBeOfType<SideSnapshot.Present>();
        var remote = conflict.Remote.ShouldBeOfType<SideSnapshot.Present>();
        (local.Size, remote.Size, remote.Tag).ShouldBe((5, 5, Option.Some(RemoteIds.CTag("c1"))));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_sizes_differ_then_it_goes_to_conflicts_without_hashing_into_a_link()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 6, HelloQuickXorHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        summary.LinkedExisting.ShouldBe(0);
        summary.Conflicts.ShouldHaveSingleItem().Kind.ShouldBe(ConflictKindFactory.CreateBothAdded());
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_linked_file_is_planned_again_then_the_second_pass_does_nothing()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);
        _ = await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken);

        var second = Summary(await fixture.RunAsync(CancellationToken));

        (second.Downloaded, second.SentToConflicts, second.LinkedExisting).ShouldBe((0, 0, 0));
    }

    private const string EmptyQuickXorHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private static byte[] Pattern(int length) => [.. Enumerable.Range(0, length).Select(index => (byte)((index * 31 + 7) % 251))];

    private static string LockedPath => "/sync/root/Docs/a.txt";

    [Theory]
    [InlineData(81919, "byNif6NoVt6rhiKRiw8FumiqYVA=")]
    [InlineData(81920, "byNif6NoVt6rhiKRdHAFumiqQUA=")]
    [InlineData(81921, "zyNif6NoVt6rhiKRdXAFumiqQUA=")]
    [InlineData(163840, "+nhA2wRcIFY4lNWKQcuAHcsLaCs=")]
    [InlineData(200000, "0lppLlE1sn/syuQ/kvKFqzJQT90=")]
    public async Task when_a_file_crosses_the_read_buffer_boundary_and_matches_the_cloud_hash_then_it_is_linked(int length, string cloudHash)
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/big.bin", string.Empty);
        fixture.FileSystem.File.WriteAllBytes("/sync/root/Docs/big.bin", Pattern(length));
        fixture.Remote.File("Docs/big.bin", "a", "c1", length, cloudHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/big.bin", length)), CancellationToken));

        (summary.LinkedExisting, summary.SentToConflicts).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_both_sides_added_an_empty_file_then_it_is_linked()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/empty.txt", string.Empty);
        fixture.Remote.File("Docs/empty.txt", "a", "c1", 0, EmptyQuickXorHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/empty.txt", 0)), CancellationToken));

        (summary.LinkedExisting, summary.SentToConflicts).ShouldBe((1, 0));
        fixture.Baseline.Has("Docs/empty.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_local_file_cannot_be_read_then_the_item_fails_and_no_conflict_is_recorded()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);
        using var handle = fixture.FileSystem.File.Open(LockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        summary.Outcomes.ShouldHaveSingleItem().ShouldBeOfType<DownloadItemOutcome.Failed>().Failure.ShouldBeOfType<DownloadFailure.Local>().Failure.ShouldBeOfType<LocalOperationFailure.Locked>();
        (summary.SentToConflicts, summary.LinkedExisting, summary.Conflicts.Count).ShouldBe((0, 0, 0));
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_local_file_was_unreadable_and_is_readable_on_the_next_pass_then_it_is_linked()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);
        var handle = fixture.FileSystem.File.Open(LockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        _ = await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken);
        handle.Dispose();

        var second = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        (second.LinkedExisting, second.SentToConflicts, second.Failed).ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task when_the_local_file_changed_size_since_the_scan_then_the_item_fails_as_target_changed_and_is_not_a_conflict()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello!");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken));

        summary.Outcomes.ShouldHaveSingleItem().ShouldBeOfType<DownloadItemOutcome.Failed>().Failure.ShouldBeOfType<DownloadFailure.Local>().Failure.ShouldBeOfType<LocalOperationFailure.TargetChanged>();
        fixture.Baseline.Has("Docs/a.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_run_is_cancelled_while_a_file_is_being_linked_then_the_cancel_propagates_and_nothing_is_linked_or_recorded()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/big.bin", string.Empty);
        fixture.FileSystem.File.WriteAllBytes("/sync/root/Docs/big.bin", Pattern(200000));
        fixture.Remote.File("Docs/big.bin", "a", "c1", 200000, "0lppLlE1sn/syuQ/kvKFqzJQT90=");
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Feed.ProgressChanged += (_, _) => cancel.Cancel();

        var run = fixture.RunAsync(Changes(AddedFile("Docs/big.bin", 200000)), cancel.Token);

        await Should.ThrowAsync<OperationCanceledException>(() => run);
        fixture.Baseline.Has("Docs/big.bin").ShouldBeFalse();
        fixture.Attention.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_file_is_linked_then_the_progress_reports_its_bytes_against_a_total_that_includes_it()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/a.txt", "hello");
        fixture.Remote.File("Docs/a.txt", "a", "c1", 5, HelloQuickXorHash);
        List<SyncProgress> seen = [];
        fixture.Feed.ProgressChanged += (_, args) => seen.Add(args.Progress);

        _ = await fixture.RunAsync(Changes(AddedFile("Docs/a.txt", 5)), CancellationToken);

        seen.ShouldContain(progress => progress.BytesDone == 5 && progress.BytesTotal == 5);
    }

    [Fact]
    public async Task when_the_baseline_cannot_be_written_with_a_link_in_the_batch_then_the_pass_stops_unexpected_and_the_link_is_not_stored()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/secret-a.txt", "hello");
        fixture.Remote.File("Docs/secret-a.txt", "a", "c1", 5, HelloQuickXorHash);
        fixture.Baseline.UpsertFails = true;

        var result = await fixture.RunAsync(Changes(AddedFile("Docs/secret-a.txt", 5)), CancellationToken);

        result.Match(_ => (object)"ran", failure => failure).ShouldBe(DownloadPassFailureFactory.CreateUnexpected());
        fixture.Baseline.Has("Docs/secret-a.txt").ShouldBeFalse();
        fixture.Logs.Logger.Messages.ShouldNotContain(message => message.Contains("secret-a", StringComparison.Ordinal) || message.Contains("/sync/root", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_cloud_path_differs_in_case_from_the_local_path_then_the_file_is_linked_under_the_local_path()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.Write("Docs/A.txt", "hello");
        fixture.Remote.File("docs/a.TXT", "a", "c1", 5, HelloQuickXorHash);

        var summary = Summary(await fixture.RunAsync(Changes(AddedFile("Docs/A.txt", 5)), CancellationToken));

        (summary.LinkedExisting, summary.SentToConflicts).ShouldBe((1, 0));
        fixture.Baseline.Find("docs/a.txt").Path.Value.ShouldBe("Docs/A.txt");
    }

    [Fact]
    public async Task when_twenty_files_both_sides_added_are_linked_in_one_pass_then_every_one_is_linked_and_stored_once()
    {
        using var fixture = await CreateAsync(CancellationToken, DownloadPassOptionsFactory.Create(4, 0, 3));
        fixture.Docs();
        List<LocalChange> added = [];
        foreach (var index in Enumerable.Range(0, 20))
        {
            _ = fixture.Write($"Docs/f{index}.txt", "hello");
            fixture.Remote.File($"Docs/f{index}.txt", $"id{index}", "c1", 5, HelloQuickXorHash);
            added.Add(AddedFile($"Docs/f{index}.txt", 5));
        }

        var summary = Summary(await Task.Run(() => fixture.RunAsync(Changes([.. added]), CancellationToken), CancellationToken));

        (summary.LinkedExisting, summary.SentToConflicts, summary.Failed).ShouldBe((20, 0, 0));
        fixture.Baseline.All.Count(entry => entry.Kind is SyncBaselineKind.File).ShouldBe(20);
    }
}
