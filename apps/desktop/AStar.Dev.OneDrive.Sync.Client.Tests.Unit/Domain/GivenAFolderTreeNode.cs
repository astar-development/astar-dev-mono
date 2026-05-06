using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFolderTreeNode
{
    [Fact]
    public void when_created_with_all_properties_then_they_are_preserved()
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
    public void when_created_with_defaults_then_sync_state_is_excluded()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs");

        node.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_created_with_defaults_then_has_children_is_true()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs");

        node.HasChildren.ShouldBeTrue();
    }

    [Fact]
    public void when_created_with_custom_state_then_sync_state_is_set()
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);

        node.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_created_with_no_children_then_has_children_is_false()
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
    public void when_created_with_any_sync_state_then_it_is_preserved(FolderSyncState state)
    {
        var node = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", state);

        node.SyncState.ShouldBe(state);
    }

    [Fact]
    public void when_root_folder_is_created_then_parent_id_is_null()
    {
        var node = new FolderTreeNode("root-id", "OneDrive", null, "account", "/OneDrive");

        node.ParentId.ShouldBeNull();
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var node1 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);
        var node2 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);

        node1.ShouldBe(node2);
    }

    [Fact]
    public void when_two_instances_have_different_states_then_they_are_not_equal()
    {
        var node1 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Included);
        var node2 = new FolderTreeNode("id", "Docs", "parent", "account", "/Docs", FolderSyncState.Excluded);

        node1.ShouldNotBe(node2);
    }

    [Fact]
    public void when_nested_structure_is_created_then_hierarchy_is_maintained()
    {
        var root = new FolderTreeNode("root-id", "OneDrive", null, "account", "/OneDrive");
        var child = new FolderTreeNode("child-id", "Documents", "root-id", "account", "/OneDrive/Documents");
        var grandchild = new FolderTreeNode("grandchild-id", "Projects", "child-id", "account", "/OneDrive/Documents/Projects");

        root.ParentId.ShouldBeNull();
        child.ParentId.ShouldBe("root-id");
        grandchild.ParentId.ShouldBe("child-id");
    }
}
