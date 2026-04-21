using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

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

        var item = new DeltaItem(
            id, driveId, name, parentId, isFolder, isDeleted, size,
            lastModified, downloadUrl, relativePath);

        item.Id.ShouldBe(id);
        item.DriveId.ShouldBe(driveId);
        item.Name.ShouldBe(name);
        item.ParentId.ShouldBe(parentId);
        item.IsFolder.ShouldBe(isFolder);
        item.IsDeleted.ShouldBe(isDeleted);
        item.Size.ShouldBe(size);
        item.LastModified.ShouldBe(lastModified);
        item.DownloadUrl.ShouldBe(downloadUrl);
        item.RelativePath.ShouldBe(relativePath);
    }

    [Fact]
    public void DeltaItem_CanBeCreated_WithoutRelativePath()
    {
        string id = "file-123";
        string driveId = "drive-456";
        string name = "document.docx";
        string parentId = "folder-789";

        var item = new DeltaItem(id, driveId, name, parentId, false, false, 2048, DateTimeOffset.UtcNow, "url");

        item.RelativePath.ShouldBeNull();
    }

    [Fact]
    public void DeltaItem_DeletedItem_ShouldHaveIsDeletedTrue()
    {
        var item = new DeltaItem("id", "drive", "name", "parent", false, true, 0, null, null);

        item.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void DeltaItem_Folder_ShouldHaveIsFolderTrue()
    {
        var item = new DeltaItem("id", "drive", "Documents", "parent", true, false, 0, null, null);

        item.IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void DeltaItem_File_ShouldHaveIsFolderFalse()
    {
        var item = new DeltaItem("id", "drive", "file.txt", "parent", false, false, 1024, DateTimeOffset.UtcNow, "url");

        item.IsFolder.ShouldBeFalse();
    }

    [Fact]
    public void DeltaItem_ZeroSize_IsValid()
    {
        var item = new DeltaItem("id", "drive", "empty.txt", "parent", false, false, 0, DateTimeOffset.UtcNow, "url");

        item.Size.ShouldBe(0);
    }

    [Fact]
    public void DeltaItem_LargeSize_IsValid()
    {
        long largeSize = 1_099_511_627_776L; // 1TB

        var item = new DeltaItem("id", "drive", "large.iso", "parent", false, false, largeSize, DateTimeOffset.UtcNow, "url");

        item.Size.ShouldBe(largeSize);
    }

    [Fact]
    public void DeltaItem_WithoutLastModified_ShouldBeNull()
    {
        var item = new DeltaItem("id", "drive", "name", "parent", false, false, 0, null, "url");

        item.LastModified.ShouldBeNull();
    }

    [Fact]
    public void DeltaItem_IsRecord_ShouldSupportValueEquality()
    {
        var item1 = new DeltaItem("id", "drive", "name", "parent", false, false, 100, DateTimeOffset.UtcNow, "url");
        var item2 = new DeltaItem("id", "drive", "name", "parent", false, false, 100, item1.LastModified, "url");

        item1.ShouldBe(item2);
    }

    [Fact]
    public void DeltaItem_DeletedFile_ShouldDifferFromNonDeleted()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var active = new DeltaItem("id", "drive", "name", "parent", false, false, 100, timestamp, "url");
        var deleted = new DeltaItem("id", "drive", "name", "parent", false, true, 100, timestamp, "url");

        active.ShouldNotBe(deleted);
    }
}

public class FolderTreeNodeTests
{
    [Fact]
    public void FolderTreeNode_CanBeCreatedWithAllProperties()
    {
        string id = "folder-123";
        string name = "Documents";
        string parentId = "folder-456";
        string accountId = "account-789";
        string remotePath = "/Documents";

        var node = new FolderTreeNode(id, name, parentId, accountId, remotePath);

        node.Id.ShouldBe(id);
        node.Name.ShouldBe(name);
        node.ParentId.ShouldBe(parentId);
        node.AccountId.ShouldBe(accountId);
    }

    [Fact]
    public void FolderTreeNode_DefaultSyncState_ShouldBeExcluded()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs");

        node.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void FolderTreeNode_DefaultHasChildren_ShouldBeTrue()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs");

        node.HasChildren.ShouldBeTrue();
    }

    [Fact]
    public void FolderTreeNode_CanBeCreatedWithCustomState()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);

        node.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void FolderTreeNode_CanBeCreatedWithNoChildren()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Excluded, HasChildren: false);

        node.HasChildren.ShouldBeFalse();
    }

    [Theory]
    [InlineData(FolderSyncState.Excluded)]
    [InlineData(FolderSyncState.Included)]
    [InlineData(FolderSyncState.Partial)]
    [InlineData(FolderSyncState.Syncing)]
    [InlineData(FolderSyncState.Synced)]
    [InlineData(FolderSyncState.Conflict)]
    [InlineData(FolderSyncState.Error)]
    public void FolderTreeNode_ShouldSupportAllSyncStates(FolderSyncState state)
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", state);

        node.SyncState.ShouldBe(state);
    }

    [Fact]
    public void FolderTreeNode_RootFolder_CanHaveNullParentId()
    {
        var node = new FolderTreeNode("root-id", "OneDrive", null, "account", "/OneDrive");

        node.ParentId.ShouldBeNull();
    }

    [Fact]
    public void FolderTreeNode_IsRecord_ShouldSupportValueEquality()
    {
        var node1 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);
        var node2 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);

        node1.ShouldBe(node2);
    }

    [Fact]
    public void FolderTreeNode_DifferentStateShouldNotBeEqual()
    {
        var node1 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);
        var node2 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Excluded);

        node1.ShouldNotBe(node2);
    }

    [Fact]
    public void FolderTreeNode_NestedFolderStructure_ShouldMaintainHierarchy()
    {
        var root = new FolderTreeNode("root-id", "OneDrive", null, "account", "/OneDrive");
        var child = new FolderTreeNode("child-id", "Documents", "root-id", "account", "/OneDrive/Documents");
        var grandchild = new FolderTreeNode("grandchild-id", "Projects", "child-id", "account", "/OneDrive/Documents/Projects");

        root.ParentId.ShouldBeNull();
        child.ParentId.ShouldBe("root-id");
        grandchild.ParentId.ShouldBe("child-id");
    }
}

public class SyncConflictTests
{
    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveUniqueId()
    {
        var conflict1 = new SyncConflict();
        var conflict2 = new SyncConflict();

        conflict1.Id.ShouldNotBe(conflict2.Id);
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHavePendingState()
    {
        var conflict = new SyncConflict();

        conflict.State.ShouldBe(ConflictState.Pending);
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveNoResolution()
    {
        var conflict = new SyncConflict();

        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveCurrentDetectedTime()
    {
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-100);

        var conflict = new SyncConflict();

        conflict.DetectedAt.ShouldBeGreaterThanOrEqualTo(before);
        conflict.DetectedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.AddMilliseconds(100));
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldNotBeResolved()
    {
        var conflict = new SyncConflict();

        conflict.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_CanSetPropertiesViaInit()
    {
        var id = Guid.NewGuid();
        string accountId = "account-123";
        string folderId = "folder-456";
        string remoteItemId = "item-789";
        string relativePath = "Documents/report.pdf";
        string localPath = "/home/jason/Documents/report.pdf";
        var now = DateTimeOffset.UtcNow;

        var conflict = new SyncConflict
        {
            Id = id,
            AccountId = accountId,
            FolderId = folderId,
            RemoteItemId = remoteItemId,
            RelativePath = relativePath,
            LocalPath = localPath,
            LocalModified = now.AddHours(-1),
            RemoteModified = now,
            LocalSize = 1024,
            RemoteSize = 2048,
            DetectedAt = now
        };

        conflict.Id.ShouldBe(id);
        conflict.AccountId.ShouldBe(accountId);
        conflict.FolderId.ShouldBe(folderId);
        conflict.RemoteItemId.ShouldBe(remoteItemId);
        conflict.RelativePath.ShouldBe(relativePath);
        conflict.LocalPath.ShouldBe(localPath);
        conflict.LocalSize.ShouldBe(1024);
        conflict.RemoteSize.ShouldBe(2048);
    }

    [Fact]
    public void SyncConflict_CanBeResolved()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.State = ConflictState.Resolved;
        conflict.Resolution = ConflictPolicy.LastWriteWins;
        conflict.ResolvedAt = DateTimeOffset.UtcNow;

        conflict.State.ShouldBe(ConflictState.Resolved);
        conflict.Resolution.ShouldBe(ConflictPolicy.LastWriteWins);
        _ = conflict.ResolvedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SyncConflict_CanBeSkipped()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.State = ConflictState.Skipped;

        conflict.State.ShouldBe(ConflictState.Skipped);
    }

    [Theory]
    [InlineData(ConflictState.Pending)]
    [InlineData(ConflictState.Resolved)]
    [InlineData(ConflictState.Skipped)]
    public void SyncConflict_ShouldSupportAllStates(ConflictState state)
    {
        var conflict = new SyncConflict { State = state };

        conflict.State.ShouldBe(state);
    }

    [Fact]
    public void SyncConflict_LocalSizeCanBeLarge()
    {
        long largeSize = 1_073_741_824L; // 1GB

        var conflict = new SyncConflict { LocalSize = largeSize };

        conflict.LocalSize.ShouldBe(largeSize);
    }

    [Fact]
    public void SyncConflict_CanTrackVersionConflict()
    {
        var now = DateTimeOffset.UtcNow;

        var conflict = new SyncConflict
        {
            LocalModified = now.AddHours(-2),
            RemoteModified = now,
            LocalSize = 1024,
            RemoteSize = 2048
        };

        conflict.LocalModified.ShouldBeLessThan(conflict.RemoteModified);
        conflict.LocalSize.ShouldNotBe(conflict.RemoteSize);
    }

    [Fact]
    public void SyncConflict_WithoutResolution_ShouldHaveNullResolution()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_WithResolution_ShouldHaveNonNullResolution()
    {
        var conflict = new SyncConflict
        {
            State = ConflictState.Resolved,
            Resolution = ConflictPolicy.LocalWins
        };

        _ = conflict.Resolution.ShouldNotBeNull();
        conflict.Resolution.ShouldBe(ConflictPolicy.LocalWins);
    }
}
