using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

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
