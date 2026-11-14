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
}
