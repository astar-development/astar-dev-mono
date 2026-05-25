using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenADeltaItem
{
    [Fact]
    public void when_file_created_then_it_is_FileDeltaItem()
    {
        var item = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("file.txt"), 100L, DateTimeOffset.UtcNow, Option.None<string>(), VersionInfoFactory.Create(null, null));

        item.ShouldBeOfType<FileDeltaItem>();
    }

    [Fact]
    public void when_folder_created_then_it_is_FolderDeltaItem()
    {
        var item = DeltaItemFactory.CreateFolder(new OneDriveItemId("id"), new DriveId("drive"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("Documents"), VersionInfoFactory.Create(null, null));

        item.ShouldBeOfType<FolderDeltaItem>();
    }

    [Fact]
    public void when_deleted_created_then_it_is_DeletedDeltaItem()
    {
        var item = DeltaItemFactory.CreateDeleted(new OneDriveItemId("id"), new DriveId("drive"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("gone.txt"));

        item.ShouldBeOfType<DeletedDeltaItem>();
    }

    [Fact]
    public void when_file_created_with_all_properties_then_they_are_preserved()
    {
        string id = "file-123";
        var driveId = new DriveId("drive-456");
        string name = "document.docx";
        string parentId = "folder-789";
        long size = 2048L;
        var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
        string downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc/items/xyz/content";
        string relativePath = "Documents/document.docx";

        var item = DeltaItemFactory.CreateFile(
            new OneDriveItemId(id),
            driveId,
            new OneDriveFolderId(parentId),
            ItemPathFactory.Create(name, relativePath),
            size,
            lastModified,
            downloadUrl,
            VersionInfoFactory.Create(null, null));

        item.ParentId.TryGetValue(out var pid).ShouldBeTrue();
        item.LastModified.TryGetValue(out var lm).ShouldBeTrue();
        item.DownloadUrl.TryGetValue(out string? dlUrl).ShouldBeTrue();
        item.Path.RelativePath.TryGetValue(out string? relPath).ShouldBeTrue();
        item.Id.Id.ShouldBe(id);
        item.DriveId.ShouldBe(driveId);
        item.Path.Name.ShouldBe(name);
        pid.Id.ShouldBe(parentId);
        item.Size.ShouldBe(size);
        lm.ShouldBe(lastModified);
        dlUrl.ShouldBe(downloadUrl);
        relPath.ShouldBe(relativePath);
    }

    [Fact]
    public void when_folder_created_with_all_properties_then_they_are_preserved()
    {
        string id = "folder-123";
        var driveId = new DriveId("drive-456");
        string name = "Documents";
        string parentId = "root-id";

        var item = DeltaItemFactory.CreateFolder(
            new OneDriveItemId(id),
            driveId,
            new OneDriveFolderId(parentId),
            ItemPathFactory.Create(name),
            VersionInfoFactory.Create("etag-1", null));

        item.ParentId.TryGetValue(out var folderId).ShouldBeTrue();
        item.VersionInfo.ETag.TryGetValue(out string? etagVal).ShouldBeTrue();
        item.Id.Id.ShouldBe(id);
        item.DriveId.ShouldBe(driveId);
        item.Path.Name.ShouldBe(name);
        folderId.Id.ShouldBe(parentId);
        etagVal.ShouldBe("etag-1");
    }

    [Fact]
    public void when_file_created_without_relative_path_then_relative_path_is_none()
    {
        var item = DeltaItemFactory.CreateFile(new OneDriveItemId("file-123"), new DriveId("drive-456"), new OneDriveFolderId("folder-789"), ItemPathFactory.Create("document.docx"), 2048, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        (item.Path.RelativePath is Option<string>.None).ShouldBeTrue();
    }

    [Fact]
    public void when_file_size_is_zero_then_it_is_preserved()
    {
        var item = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), new OneDriveFolderId("parent"), ItemPathFactory.Create("empty.txt"), 0, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.Size.ShouldBe(0);
    }

    [Fact]
    public void when_file_size_is_large_then_it_is_preserved()
    {
        long largeSize = 1_099_511_627_776L;

        var item = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), new OneDriveFolderId("parent"), ItemPathFactory.Create("large.iso"), largeSize, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.Size.ShouldBe(largeSize);
    }

    [Fact]
    public void when_file_last_modified_is_none_then_it_is_none()
    {
        var item = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), 0, Option.None<DateTimeOffset>(), "url", VersionInfoFactory.Create(null, null));

        (item.LastModified is Option<DateTimeOffset>.None).ShouldBeTrue();
    }

    [Fact]
    public void when_two_file_instances_have_same_values_then_they_are_equal()
    {
        var item1 = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), 100, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));
        var item2 = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), 100, item1.LastModified, "url", VersionInfoFactory.Create(null, null));

        item1.ShouldBe(item2);
    }

    [Fact]
    public void when_file_and_folder_have_same_base_properties_then_they_are_not_equal()
    {
        var fileItem = DeltaItemFactory.CreateFile(new OneDriveItemId("id"), new DriveId("drive"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("name"), 0, Option.None<DateTimeOffset>(), Option.None<string>(), VersionInfoFactory.Create(null, null));
        var folderItem = DeltaItemFactory.CreateFolder(new OneDriveItemId("id"), new DriveId("drive"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create("name"), VersionInfoFactory.Create(null, null));

        ((DeltaItem)fileItem).ShouldNotBe((DeltaItem)folderItem);
    }

    [Fact]
    public void when_deleted_item_created_then_properties_are_preserved()
    {
        var id = new OneDriveItemId("deleted-1");
        var driveId = new DriveId("drive-1");

        var item = DeltaItemFactory.CreateDeleted(id, driveId, Option.None<OneDriveFolderId>(), ItemPathFactory.Create("gone.txt"));

        item.Id.ShouldBe(id);
        item.DriveId.ShouldBe(driveId);
    }
}
