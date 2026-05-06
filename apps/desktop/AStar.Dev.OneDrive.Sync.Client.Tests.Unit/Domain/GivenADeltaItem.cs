using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenADeltaItem
{
    [Fact]
    public void when_created_with_all_properties_then_they_are_preserved()
    {
        string id = "file-123";
        string driveId = "drive-456";
        string name = "document.docx";
        string parentId = "folder-789";
        bool isFolder = false;
        bool isDeleted = false;
        long size = 2048L;
        var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
        string downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc/items/xyz/content";
        string relativePath = "Documents/document.docx";

        var item = DeltaItemFactory.Create(
            new OneDriveItemId(id),
            driveId,
            new OneDriveFolderId(parentId),
            ItemPathFactory.Create(name, relativePath),
            isFolder,
            isDeleted,
            size,
            lastModified,
            downloadUrl,
            VersionInfoFactory.Create(null, null));

        item.Id.Id.ShouldBe(id);
        item.DriveId.ShouldBe(driveId);
        item.Path.Name.ShouldBe(name);
        item.ParentId?.Id.ShouldBe(parentId);
        item.IsFolder.ShouldBe(isFolder);
        item.IsDeleted.ShouldBe(isDeleted);
        item.Size.ShouldBe(size);
        item.LastModified.ShouldBe(lastModified);
        item.DownloadUrl.ShouldBe(downloadUrl);
        item.Path.RelativePath.ShouldBe(relativePath);
    }

    [Fact]
    public void when_created_without_relative_path_then_relative_path_is_null()
    {
        var item = DeltaItemFactory.Create(
            new OneDriveItemId("file-123"),
            "drive-456",
            new OneDriveFolderId("folder-789"),
            ItemPathFactory.Create("document.docx"),
            false,
            false,
            2048,
            DateTimeOffset.UtcNow,
            "url",
            VersionInfoFactory.Create(null, null));

        item.Path.RelativePath.ShouldBeNull();
    }

    [Fact]
    public void when_created_as_deleted_then_is_deleted_is_true()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", null, ItemPathFactory.Create("name"), false, true, 0, null, null, VersionInfoFactory.Create(null, null));

        item.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void when_created_as_folder_then_is_folder_is_true()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("Documents"), true, false, 0, null, null, VersionInfoFactory.Create(null, null));

        item.IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void when_created_as_file_then_is_folder_is_false()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("file.txt"), false, false, 1024, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.IsFolder.ShouldBeFalse();
    }

    [Fact]
    public void when_size_is_zero_then_it_is_preserved()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("empty.txt"), false, false, 0, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.Size.ShouldBe(0);
    }

    [Fact]
    public void when_size_is_large_then_it_is_preserved()
    {
        long largeSize = 1_099_511_627_776L;

        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("large.iso"), false, false, largeSize, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.Size.ShouldBe(largeSize);
    }

    [Fact]
    public void when_last_modified_is_null_then_it_is_null()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 0, null, "url", VersionInfoFactory.Create(null, null));

        item.LastModified.ShouldBeNull();
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var item1 = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 100, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));
        var item2 = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 100, item1.LastModified, "url", VersionInfoFactory.Create(null, null));

        item1.ShouldBe(item2);
    }

    [Fact]
    public void when_deleted_flag_differs_then_instances_are_not_equal()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var active = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 100, timestamp, "url", VersionInfoFactory.Create(null, null));
        var deleted = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, true, 100, timestamp, "url", VersionInfoFactory.Create(null, null));

        active.ShouldNotBe(deleted);
    }
}
