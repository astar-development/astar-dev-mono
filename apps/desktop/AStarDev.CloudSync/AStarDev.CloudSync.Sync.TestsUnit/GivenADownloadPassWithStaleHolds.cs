using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADownloadPassWithStaleHolds
{
    private const int Items = 100;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string File(string folder, int index) => $"{folder}/f{index:000}.txt";

    private static IEnumerable<string> Files(string folder, int from, int count) => Enumerable.Range(from, count).Select(index => File(folder, index));

    private static void SyncPair(DownloadPassFixture fixture, string folder, int stillRemote)
    {
        _ = fixture.SyncedFolder(folder, $"remote-{folder.ToUpperInvariant()}");
        fixture.Remote.Folder(folder);
        for (var index = 0; index < Items; index++)
        {
            _ = fixture.Synced(File(folder, index), "x", $"{folder}-{index}");
            if (index >= Items - stillRemote)
                fixture.Remote.File(File(folder, index), $"{folder}-{index}", "c1", 1);
        }
    }

    [Fact]
    public async Task when_the_remote_items_of_a_held_pair_are_back_then_its_held_local_deletions_are_closed()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 100);
        var held = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteLocal(), Files("Docs", 0, 60));

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        fixture.Attention.Closed.ShouldBe([held]);
        summary.DeletedLocally.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_held_pair_drops_below_the_threshold_then_its_set_is_closed_and_nothing_is_deleted_in_that_pass()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 70);
        var held = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteLocal(), Files("Docs", 0, 60));

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        fixture.Attention.Closed.ShouldBe([held]);
        summary.DeletedLocally.ShouldBe(0);
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").Length.ShouldBe(Items);
    }

    [Fact]
    public async Task when_a_held_pair_would_still_trip_the_guard_then_its_set_stays_open_and_nothing_is_deleted()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 40);
        _ = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteLocal(), Files("Docs", 0, 60));

        var summary = Summary(await fixture.RunAsync(CancellationToken));

        fixture.Attention.Closed.ShouldBeEmpty();
        fixture.Attention.Held.ShouldBeEmpty();
        summary.DeletedLocally.ShouldBe(0);
        fixture.FileSystem.Directory.GetFiles("/sync/root/Docs").Length.ShouldBe(Items);
    }

    [Fact]
    public async Task when_the_same_pair_has_held_remote_deletions_then_that_set_is_left_open()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 100);
        _ = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteRemote(), Files("Docs", 0, 60));

        _ = Summary(await fixture.RunAsync(CancellationToken));

        fixture.Attention.Closed.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_one_pair_recovers_and_another_is_still_held_then_only_the_recovered_pair_is_closed()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([SelectionTestRules.Include("Docs"), SelectionTestRules.Include("Photos")]);
        SyncPair(fixture, "Docs", stillRemote: 100);
        SyncPair(fixture, "Photos", stillRemote: 40);
        var recovered = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteLocal(), Files("Docs", 0, 60));
        _ = fixture.Hold("Photos", DeletionDirectionFactory.CreateDeleteLocal(), Files("Photos", 0, 60));

        _ = Summary(await fixture.RunAsync(CancellationToken));

        fixture.Attention.Closed.ShouldBe([recovered]);
        fixture.FileSystem.Directory.GetFiles("/sync/root/Photos").Length.ShouldBe(Items);
    }

    [Fact]
    public async Task when_a_resync_is_in_progress_then_no_held_set_is_closed()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 100);
        _ = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteLocal(), Files("Docs", 0, 60));
        fixture.Quarantine.Snapshot = QuarantineSnapshotFactory.CreateResyncing(fixture.Quarantine.Snapshot.Entries);

        _ = Summary(await fixture.RunAsync(CancellationToken));

        fixture.Attention.Closed.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_recovered_set_cannot_be_closed_then_the_pass_fails_and_changes_nothing()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 100);
        _ = fixture.Hold("Docs", DeletionDirectionFactory.CreateDeleteLocal(), Files("Docs", 0, 60));
        fixture.Remote.File("Docs/new.txt", "new", "c2", 5);
        fixture.Attention.CloseFails = true;

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.Unexpected).ShouldBeTrue();
        fixture.FileSystem.File.Exists("/sync/root/Docs/new.txt").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_open_sets_cannot_be_read_then_the_pass_fails_and_changes_nothing()
    {
        using var fixture = await CreateAsync(CancellationToken);
        SyncPair(fixture, "Docs", stillRemote: 100);
        fixture.Remote.File("Docs/new.txt", "new", "c2", 5);
        fixture.Attention.ReadFails = true;

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => false, failure => failure is DownloadPassFailure.Unexpected).ShouldBeTrue();
        fixture.FileSystem.File.Exists("/sync/root/Docs/new.txt").ShouldBeFalse();
    }
}
