using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveFolderId = AStar.Dev.OneDrive.Sync.Client.Domain.OneDriveFolderId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenASyncedItemEntityFactory
{
    private static FileDeltaItem CreateDeltaItem(string? etag, string? ctag)
        => DeltaItemFactory.CreateFile(new OneDriveItemId("item-1"), new DriveId("drive-1"), null, ItemPathFactory.Create("file.txt", "/file.txt"), 100L, DateTimeOffset.UtcNow.AddDays(-1), null, VersionInfoFactory.Create(etag, ctag));

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

    [Fact]
    public void when_creating_from_download_job_with_etag_in_metadata_then_tags_etag_is_populated()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "/file.txt");
        var versionInfo = VersionInfoFactory.Create("etag-abc", null);
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1), versionInfo);
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(new AccountId("acc-1"), job, "/file.txt");

        entity.Tags.ETag.ShouldBe("etag-abc");
    }

    [Fact]
    public void when_creating_from_download_job_with_null_version_info_then_tags_etag_is_null()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "/file.txt");
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(new AccountId("acc-1"), job, "/file.txt");

        entity.Tags.ETag.ShouldBeNull();
    }
}
