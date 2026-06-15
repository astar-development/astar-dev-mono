using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveFolderId = AStar.Dev.OneDrive.Sync.Client.Domain.OneDriveFolderId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenASyncedItemEntityFactory
{
    private static FileDeltaItem CreateDeltaItem(Option<string> etag, Option<string> ctag)
        => DeltaItemFactory.CreateFile(new OneDriveItemId("item-1"), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("file.txt", "/file.txt"), 100L, DateTimeOffset.UtcNow.AddDays(-1), Option.None<string>(), VersionInfoFactory.Create(etag, ctag));

    [Fact]
    public void when_creating_from_delta_item_then_tags_etag_is_populated()
    {
        var item = CreateDeltaItem("etag-abc", Option.None<string>());

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.Tags.ETag.TryGetValue(out string? etag1).ShouldBeTrue();
        etag1.ShouldBe("etag-abc");
    }

    [Fact]
    public void when_creating_from_delta_item_then_tags_ctag_is_populated()
    {
        var item = CreateDeltaItem(Option.None<string>(), "ctag-xyz");

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.Tags.CTag.TryGetValue(out string? ctag1).ShouldBeTrue();
        ctag1.ShouldBe("ctag-xyz");
    }

    [Fact]
    public void when_creating_from_delta_item_with_null_etag_then_tags_etag_is_none()
    {
        var item = CreateDeltaItem(Option.None<string>(), "ctag-xyz");

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        (entity.Tags.ETag is Option<string>.None).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_from_delta_item_with_null_ctag_then_tags_ctag_is_none()
    {
        var item = CreateDeltaItem("etag-abc", Option.None<string>());

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        (entity.Tags.CTag is Option<string>.None).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_from_download_job_with_etag_in_metadata_then_tags_etag_is_populated()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "/file.txt");
        var versionInfo = VersionInfoFactory.Create("etag-abc", Option.None<string>());
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1), Option.Some(versionInfo));
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(new AccountId("acc-1"), job, "/file.txt");

        entity.Tags.ETag.TryGetValue(out string? etag2).ShouldBeTrue();
        etag2.ShouldBe("etag-abc");
    }

    [Fact]
    public void when_creating_from_download_job_with_null_version_info_then_tags_etag_is_none()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "/file.txt");
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(new AccountId("acc-1"), job, "/file.txt");

        (entity.Tags.ETag is Option<string>.None).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_from_upload_job_with_uploaded_id_then_remote_item_id_matches()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile("/local/file.txt").Which(m => m.HasStringContent("content"));

        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("original-id"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "file.txt");
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));
        var job = SyncJobFactory.CreateUpload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromUploadJob(new AccountId("acc-1"), job, "uploaded-remote-id", "/file.txt", mockFileSystem);

        entity.RemoteItemId.Id.ShouldBe("uploaded-remote-id");
    }

    [Fact]
    public void when_creating_from_upload_job_then_local_path_matches_job_target()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile("/local/file.txt").Which(m => m.HasStringContent("content"));

        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("original-id"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "file.txt");
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));
        var job = SyncJobFactory.CreateUpload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromUploadJob(new AccountId("acc-1"), job, "uploaded-remote-id", "/file.txt", mockFileSystem);

        entity.LocalPath.ShouldBe("/local/file.txt");
    }

    [Fact]
    public void when_creating_from_file_delta_item_then_size_in_bytes_is_set()
    {
        var item = CreateDeltaItem(Option.None<string>(), Option.None<string>());

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/file.txt", "/local/file.txt");

        entity.SizeInBytes.ShouldBe(100L);
    }

    [Fact]
    public void when_creating_from_folder_delta_item_then_size_in_bytes_is_null()
    {
        var item = DeltaItemFactory.CreateFolder(new OneDriveItemId("folder-1"), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("docs"), VersionInfoFactory.Create(null, null));

        var entity = SyncedItemEntityFactory.Create(new AccountId("acc-1"), item, "/docs", "/local/docs");

        entity.SizeInBytes.ShouldBeNull();
    }

    [Fact]
    public void when_creating_from_download_job_then_size_in_bytes_matches_metadata()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "/file.txt");
        var metadata = SyncFileMetadataFactory.Create(4096L, DateTimeOffset.UtcNow.AddDays(-1));
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);

        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(new AccountId("acc-1"), job, "/file.txt");

        entity.SizeInBytes.ShouldBe(4096L);
    }

    [Fact]
    public void when_creating_from_upload_job_then_size_in_bytes_matches_file_on_disk()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile("/local/file.txt").Which(m => m.HasStringContent("hello world"));

        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("original-id"));
        var target = SyncFileTargetFactory.Create("/local/file.txt", "file.txt");
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));
        var job = SyncJobFactory.CreateUpload(remote, target, metadata);
        long expectedSize = mockFileSystem.FileInfo.New("/local/file.txt").Length;

        var entity = SyncedItemEntityFactory.CreateFromUploadJob(new AccountId("acc-1"), job, "uploaded-remote-id", "/file.txt", mockFileSystem);

        entity.SizeInBytes.ShouldBe(expectedSize);
    }
}
