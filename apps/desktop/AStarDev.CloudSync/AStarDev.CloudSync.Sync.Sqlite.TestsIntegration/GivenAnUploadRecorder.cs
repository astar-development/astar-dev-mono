using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnUploadRecorder : IAsyncLifetime
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private static SyncBaselineEntry BaselineOf(string path, string itemId, string cTag, string eTag = "e-1") =>
        SyncBaselineEntryFactory.Create(Account, SyncPaths.Of(path), Id(itemId), SyncBaselineKindFactory.CreateFile(10, WrittenAt, ContentHashFactory.Create("sha").Match(hash => hash, _ => throw new InvalidOperationException()), Tag(cTag), ETag(eTag)));

    private static RemoteItemSnapshot SnapshotOf(string itemId, string parent, string name, string cTag, string eTag = "e-1") =>
        RemoteItemSnapshotFactory.Create(Id(itemId), Option.Some(Id(parent)), name, RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), Tag(cTag)), WrittenAt, ETag(eTag));

    [Fact]
    public async Task when_an_upload_is_recorded_then_the_baseline_row_and_the_remote_index_row_both_exist()
    {
        _ = await Database.IndexWriter.ApplyPageAsync(Account, Caught("c0", Folder("docs", "root", "Docs")), CancellationToken);

        var recorded = await Database.UploadRecorder.RecordAsync(Account, BaselineOf("Docs/a.txt", "item-1", "c-new"), SnapshotOf("item-1", "docs", "a.txt", "c-new"), CancellationToken);

        recorded.Match(_ => true, _ => false).ShouldBeTrue();
        var baseline = await Database.BaselineReader.GetAsync(Account, SyncPaths.Of("Docs/a.txt"), CancellationToken);
        baseline.Match(option => option.Match(entry => ((SyncBaselineKind.File)entry.Kind).CTag.Value, () => "none"), _ => "failed").ShouldBe("c-new");
        var remote = await Database.IndexReader.GetAsync(Account, Id("item-1"), CancellationToken);
        remote.Match(option => option.Match(item => ((RemoteItemKind.File)item.Kind).CTag.Value, () => "none"), _ => "failed").ShouldBe("c-new");
    }

    [Fact]
    public async Task when_a_created_folder_is_recorded_then_its_baseline_row_and_remote_index_row_both_exist_and_the_next_delta_sees_no_change()
    {
        _ = await Database.IndexWriter.ApplyPageAsync(Account, Caught("c0", Folder("docs", "root", "Docs")), CancellationToken);
        var baselineRow = SyncBaselineEntryFactory.Create(Account, SyncPaths.Of("Docs/New"), Id("new-folder"), SyncBaselineKindFactory.CreateFolder());
        var indexRow = RemoteItemSnapshotFactory.Create(Id("new-folder"), Option.Some(Id("docs")), "New", RemoteItemKindFactory.CreateFolder(), WrittenAt, ETag("fe-new"));

        var recorded = await Database.UploadRecorder.RecordAsync(Account, baselineRow, indexRow, CancellationToken);

        recorded.Match(_ => true, _ => false).ShouldBeTrue();
        var baseline = await Database.BaselineReader.GetAsync(Account, SyncPaths.Of("Docs/New"), CancellationToken);
        baseline.Match(option => option.Match(entry => entry.Kind is SyncBaselineKind.Folder, () => false), _ => false).ShouldBeTrue();
        var remote = await Database.IndexReader.GetAsync(Account, Id("new-folder"), CancellationToken);
        remote.Match(option => option.Match(item => item.ETag.Value, () => "none"), _ => "failed").ShouldBe("fe-new");
        var echo = await Database.IndexWriter.ApplyPageAsync(Account, Caught("c1", indexRow), CancellationToken);
        echo.Match(delta => delta.Changes.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_the_next_delta_returns_the_item_we_just_uploaded_then_the_index_sees_no_change()
    {
        _ = await Database.IndexWriter.ApplyPageAsync(Account, Caught("c0", Folder("docs", "root", "Docs")), CancellationToken);
        _ = await Database.UploadRecorder.RecordAsync(Account, BaselineOf("Docs/a.txt", "item-1", "c-new"), SnapshotOf("item-1", "docs", "a.txt", "c-new"), CancellationToken);

        var echo = await Database.IndexWriter.ApplyPageAsync(Account, Caught("c1", SnapshotOf("item-1", "docs", "a.txt", "c-new")), CancellationToken);

        echo.Match(delta => delta.Changes.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_the_same_item_is_recorded_again_with_a_new_tag_then_both_rows_are_updated_in_place()
    {
        _ = await Database.IndexWriter.ApplyPageAsync(Account, Caught("c0", Folder("docs", "root", "Docs")), CancellationToken);
        _ = await Database.UploadRecorder.RecordAsync(Account, BaselineOf("Docs/a.txt", "item-1", "c-1"), SnapshotOf("item-1", "docs", "a.txt", "c-1"), CancellationToken);

        _ = await Database.UploadRecorder.RecordAsync(Account, BaselineOf("docs/A.TXT", "item-1", "c-2"), SnapshotOf("item-1", "docs", "a.txt", "c-2"), CancellationToken);

        var baseline = await Database.BaselineReader.GetUnderAsync(Account, SyncPaths.Of("Docs"), Option.None<RelativeSyncPath>(), 100, CancellationToken);
        baseline.Match(entries => entries.Select(entry => ((SyncBaselineKind.File)entry.Kind).CTag.Value), _ => []).ShouldBe(["c-2"]);
        var children = await Database.IndexReader.GetChildrenAsync(Account, Option.Some(Id("docs")), CancellationToken);
        children.Match(items => items.Select(item => ((RemoteItemKind.File)item.Kind).CTag.Value), _ => []).ShouldBe(["c-2"]);
    }

    [Fact]
    public async Task when_the_upload_is_recorded_with_a_cancelled_token_then_neither_row_is_written()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = () => Database.UploadRecorder.RecordAsync(Account, BaselineOf("a.txt", "item-1", "c-1"), SnapshotOf("item-1", "root", "a.txt", "c-1"), cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
        var baseline = await Database.BaselineReader.GetAsync(Account, SyncPaths.Of("a.txt"), CancellationToken);
        baseline.Match(option => option.Match(_ => "found", () => "none"), _ => "failed").ShouldBe("none");
        var remote = await Database.IndexReader.GetAsync(Account, Id("item-1"), CancellationToken);
        remote.Match(option => option.Match(_ => "found", () => "none"), _ => "failed").ShouldBe("none");
    }

    [Fact]
    public async Task when_the_recorder_is_resolved_then_it_is_registered_as_a_singleton_by_the_sync_persistence_module()
    {
        var first = Database.UploadRecorder;

        Database.UploadRecorder.ShouldBeSameAs(first);
        await Task.CompletedTask;
    }
}
