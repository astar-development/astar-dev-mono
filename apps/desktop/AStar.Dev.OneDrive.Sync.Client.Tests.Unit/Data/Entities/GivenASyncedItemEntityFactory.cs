using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenASyncedItemEntityFactory
{
    private static DeltaItem CreateDeltaItem(string? etag, string? ctag)
        => DeltaItemFactory.Create(new OneDriveItemId("item-1"), new DriveId("drive-1"), null, ItemPathFactory.Create("file.txt", "/file.txt"), false, false, 100L, DateTimeOffset.UtcNow.AddDays(-1), null, VersionInfoFactory.Create(etag, ctag));

    [Fact]
    public void when_creating_from_delta_item_then_tags_etag_is_populated()
    {
        var item = CreateDeltaItem("etag-abc", null);

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.Tags.ETag.ShouldBe("etag-abc");
    }

    [Fact]
    public void when_creating_from_delta_item_then_tags_ctag_is_populated()
    {
        var item = CreateDeltaItem(null, "ctag-xyz");

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.Tags.CTag.ShouldBe("ctag-xyz");
    }

    [Fact]
    public void when_creating_from_delta_item_with_null_etag_then_tags_etag_is_null()
    {
        var item = CreateDeltaItem(null, "ctag-xyz");

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.Tags.ETag.ShouldBeNull();
    }

    [Fact]
    public void when_creating_from_delta_item_with_null_ctag_then_tags_ctag_is_null()
    {
        var item = CreateDeltaItem("etag-abc", null);

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.Tags.CTag.ShouldBeNull();
    }
}
