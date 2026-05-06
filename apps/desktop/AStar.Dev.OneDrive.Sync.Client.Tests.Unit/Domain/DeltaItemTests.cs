using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class DeltaItemTests
{
    [Fact]
    public void DeltaItem_AllPropertiesCanBeSet()
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
    public void DeltaItem_CanBeCreated_WithoutRelativePath()
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
    public void DeltaItem_DeletedItem_ShouldHaveIsDeletedTrue()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", null, ItemPathFactory.Create("name"), false, true, 0, null, null, VersionInfoFactory.Create(null, null));

        item.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void DeltaItem_Folder_ShouldHaveIsFolderTrue()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("Documents"), true, false, 0, null, null, VersionInfoFactory.Create(null, null));

        item.IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void DeltaItem_File_ShouldHaveIsFolderFalse()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("file.txt"), false, false, 1024, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.IsFolder.ShouldBeFalse();
    }

    [Fact]
    public void DeltaItem_ZeroSize_IsValid()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("empty.txt"), false, false, 0, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.Size.ShouldBe(0);
    }

    [Fact]
    public void DeltaItem_LargeSize_IsValid()
    {
        long largeSize = 1_099_511_627_776L;

        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("large.iso"), false, false, largeSize, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));

        item.Size.ShouldBe(largeSize);
    }

    [Fact]
    public void DeltaItem_WithoutLastModified_ShouldBeNull()
    {
        var item = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 0, null, "url", VersionInfoFactory.Create(null, null));

        item.LastModified.ShouldBeNull();
    }

    [Fact]
    public void DeltaItem_IsRecord_ShouldSupportValueEquality()
    {
        var item1 = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 100, DateTimeOffset.UtcNow, "url", VersionInfoFactory.Create(null, null));
        var item2 = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 100, item1.LastModified, "url", VersionInfoFactory.Create(null, null));

        item1.ShouldBe(item2);
    }

    [Fact]
    public void DeltaItem_DeletedFile_ShouldDifferFromNonDeleted()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var active = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, false, 100, timestamp, "url", VersionInfoFactory.Create(null, null));
        var deleted = DeltaItemFactory.Create(new OneDriveItemId("id"), "drive", new OneDriveFolderId("parent"), ItemPathFactory.Create("name"), false, true, 100, timestamp, "url", VersionInfoFactory.Create(null, null));

        active.ShouldNotBe(deleted);
    }
}
