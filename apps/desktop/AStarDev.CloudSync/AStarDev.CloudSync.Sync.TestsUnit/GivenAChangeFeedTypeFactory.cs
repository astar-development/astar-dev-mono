using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAChangeFeedTypeFactory
{
    private static readonly DateTimeOffset Modified = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void when_a_file_snapshot_is_created_then_its_name_is_trimmed_and_tags_kept()
    {
        var kind = RemoteItemKindFactory.CreateFile(2_048, Option.Some("hash=="), "ctag-1");

        var snapshot = RemoteItemSnapshotFactory.Create(RemoteIds.Of("file-1"), Option.Some(RemoteIds.Of("folder-1")), " Report.docx ", kind, Modified, "etag-1");

        (snapshot.Name, snapshot.Kind, snapshot.ETag, snapshot.LastModified, snapshot.ParentId).ShouldBe(("Report.docx", kind, "etag-1", Modified, Option.Some(RemoteIds.Of("folder-1"))));
    }

    [Fact]
    public void when_a_snapshot_has_no_name_or_etag_then_they_are_empty()
    {
        var snapshot = RemoteItemSnapshotFactory.Create(RemoteIds.Of("root"), Option.None<RemoteItemId>(), null, RemoteItemKindFactory.CreateFolder(), Modified, null);

        (snapshot.Name, snapshot.ETag).ShouldBe((string.Empty, string.Empty));
    }

    [Fact]
    public void when_a_snapshot_is_printed_then_its_name_is_redacted()
    {
        var snapshot = RemoteItemSnapshotFactory.Create(RemoteIds.Of("file-1"), Option.None<RemoteItemId>(), "jane.doe@example.com.pdf", RemoteItemKindFactory.CreateDeleted(), Modified, "etag");

        snapshot.ToString().ShouldNotContain("jane.doe");
    }

    [Fact]
    public void when_a_file_kind_has_a_negative_size_or_no_ctag_then_they_are_normalised()
    {
        var kind = RemoteItemKindFactory.CreateFile(-10, Option.None<string>(), null);

        var file = kind.ShouldBeOfType<RemoteItemKind.File>();
        (file.Size, file.CTag, file.Hash).ShouldBe((0L, string.Empty, Option.None<string>()));
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
        var items = new List<RemoteItemSnapshot> { RemoteItemSnapshotFactory.Create(RemoteIds.Of("a"), Option.None<RemoteItemId>(), "a", RemoteItemKindFactory.CreateFolder(), Modified, "e") };

        var page = RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateMorePages(RemoteIds.Cursor("next")));
        items.Clear();

        (page.Items.Count, page.Next.ShouldBeOfType<PageContinuation.MorePages>().Next).ShouldBe((1, RemoteIds.Cursor("next")));
    }

    [Fact]
    public void when_each_change_is_created_then_it_carries_its_item()
    {
        var item = RemoteItemSnapshotFactory.Create(RemoteIds.Of("a"), Option.None<RemoteItemId>(), "a", RemoteItemKindFactory.CreateFolder(), Modified, "e");

        var changes = new[] { RemoteChangeFactory.CreateAdded(item), RemoteChangeFactory.CreateModified(item), RemoteChangeFactory.CreateMoved(item), RemoteChangeFactory.CreateDeleted(RemoteIds.Of("a")) };

        changes.Select(change => change.GetType().Name).ShouldBe(["Added", "Modified", "Moved", "Deleted"]);
        changes[3].ShouldBeOfType<RemoteChange.Deleted>().Id.ShouldBe(RemoteIds.Of("a"));
    }
}
