using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAChangeFeedTypeFactory
{
    private static readonly DateTimeOffset Modified = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void when_a_file_snapshot_is_created_then_its_name_is_trimmed_and_tags_kept()
    {
        var kind = RemoteItemKindFactory.CreateFile(2_048, Option.Some(RemoteIds.Hash("hash==")), RemoteIds.CTag("ctag-1"));

        var snapshot = RemoteItemSnapshotFactory.Create(RemoteIds.Of("file-1"), Option.Some(RemoteIds.Of("folder-1")), " Report.docx ", kind, Modified, RemoteIds.ETag("etag-1"));

        (snapshot.Name, snapshot.Kind, snapshot.ETag, snapshot.LastModified, snapshot.ParentId).ShouldBe(("Report.docx", kind, RemoteIds.ETag("etag-1"), Modified, Option.Some(RemoteIds.Of("folder-1"))));
    }

    [Fact]
    public void when_a_snapshot_has_no_name_then_the_name_is_empty()
    {
        var snapshot = RemoteItemSnapshotFactory.Create(RemoteIds.Of("root"), Option.None<RemoteItemId>(), null, RemoteItemKindFactory.CreateFolder(), Modified, RemoteIds.ETag("etag"));

        snapshot.Name.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_snapshot_is_printed_then_its_name_is_redacted()
    {
        var snapshot = RemoteItemSnapshotFactory.Create(RemoteIds.Of("file-1"), Option.None<RemoteItemId>(), "jane.doe@example.com.pdf", RemoteItemKindFactory.CreateDeleted(), Modified, RemoteIds.ETag("etag"));

        snapshot.ToString().ShouldNotContain("jane.doe");
    }

    [Fact]
    public void when_a_file_kind_has_a_negative_size_then_the_size_is_zero()
    {
        var kind = RemoteItemKindFactory.CreateFile(-10, Option.None<ContentHash>(), RemoteIds.CTag("ctag"));

        var file = kind.ShouldBeOfType<RemoteItemKind.File>();
        (file.Size, file.CTag, file.Hash).ShouldBe((0L, RemoteIds.CTag("ctag"), Option.None<ContentHash>()));
    }

    [Fact]
    public void when_a_page_has_no_items_then_the_list_is_empty()
    {
        var page = RemoteChangePageFactory.Create(null, PageContinuationFactory.CreateCaughtUp(RemoteIds.Cursor("delta")));

        (page.Items.Count, page.Next).ShouldBe((0, PageContinuationFactory.CreateCaughtUp(RemoteIds.Cursor("delta"))));
    }

    [Fact]
    public void when_a_page_is_created_then_it_copies_the_items()
    {
        var items = new List<RemoteItemSnapshot> { Folder("a") };

        var page = RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateMorePages(RemoteIds.Cursor("next")));
        items.Clear();

        (page.Items.Count, page.Next.ShouldBeOfType<PageContinuation.MorePages>().Next).ShouldBe((1, RemoteIds.Cursor("next")));
    }

    [Fact]
    public void when_each_change_is_created_then_it_carries_its_item()
    {
        var item = Folder("a");

        var changes = new[] { RemoteChangeFactory.CreateAdded(item), RemoteChangeFactory.CreateModified(item), RemoteChangeFactory.CreateMoved(item, Option.None<RemoteItemId>(), "old"), RemoteChangeFactory.CreateDeleted(RemoteIds.Of("a")) };

        changes.Select(change => change.GetType().Name).ShouldBe(["Added", "Modified", "Moved", "Deleted"]);
        changes[3].ShouldBeOfType<RemoteChange.Deleted>().Id.ShouldBe(RemoteIds.Of("a"));
    }

    [Fact]
    public void when_a_move_is_created_then_it_keeps_the_old_parent_and_trimmed_old_name()
    {
        var moved = RemoteChangeFactory.CreateMoved(Folder("a"), Option.Some(RemoteIds.Of("old-parent")), "  Old Name ").ShouldBeOfType<RemoteChange.Moved>();

        (moved.OldParentId, moved.OldName).ShouldBe((Option.Some(RemoteIds.Of("old-parent")), "Old Name"));
    }

    [Fact]
    public void when_a_move_has_no_old_name_then_it_is_empty_and_printing_it_reveals_no_name()
    {
        var moved = RemoteChangeFactory.CreateMoved(Folder("a"), Option.None<RemoteItemId>(), null);

        (moved.ShouldBeOfType<RemoteChange.Moved>().OldName, RemoteChangeFactory.CreateMoved(Folder("a"), Option.None<RemoteItemId>(), "Jane Tax").ToString().Contains("Jane", StringComparison.Ordinal)).ShouldBe((string.Empty, false));
    }

    private static RemoteItemSnapshot Folder(string id) => RemoteItemSnapshotFactory.Create(RemoteIds.Of(id), Option.None<RemoteItemId>(), id, RemoteItemKindFactory.CreateFolder(), Modified, RemoteIds.ETag("e"));
}
