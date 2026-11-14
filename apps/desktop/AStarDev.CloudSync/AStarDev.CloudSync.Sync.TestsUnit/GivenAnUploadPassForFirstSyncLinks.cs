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

    public void Dispose() => fixture.Dispose();
}
