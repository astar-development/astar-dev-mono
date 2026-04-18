using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFolderTreeNodeViewModel
{
    private const string FolderId     = "folder-1";
    private const string FolderName   = "Documents";
    private const string ChildId      = "child-1";
    private const string ChildName    = "Work";
    private const string GrandChildId = "grandchild-1";

    private static FolderTreeNodeViewModel BuildNode(FolderSyncState syncState = FolderSyncState.Excluded, bool hasChildren = false)
    {
        var node = new FolderTreeNode(FolderId, FolderName, null, string.Empty, syncState, HasChildren: hasChildren);

        return new FolderTreeNodeViewModel(node);
    }

    private static FolderTreeNodeViewModel BuildChildNode(string id, string name, string parentId, FolderSyncState syncState = FolderSyncState.Excluded)
    {
        var node = new FolderTreeNode(id, name, parentId, string.Empty, syncState);

        return new FolderTreeNodeViewModel(node);
    }

    [Fact]
    public void when_toggled_to_included_then_sync_state_is_included()
    {
        var sut = BuildNode(FolderSyncState.Excluded);

        sut.ToggleIncludeCommand.Execute(null);

        sut.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_toggled_to_excluded_then_sync_state_is_excluded()
    {
        var sut = BuildNode(FolderSyncState.Included);

        sut.ToggleIncludeCommand.Execute(null);

        sut.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_included_then_pre_loaded_children_are_set_to_included()
    {
        var sut   = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        sut.ToggleIncludeCommand.Execute(null);

        child.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_included_then_pre_loaded_grandchildren_are_set_to_included()
    {
        var sut        = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child      = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        var grandChild = BuildChildNode(GrandChildId, "Sub", ChildId, FolderSyncState.Excluded);
        child.AddChild(grandChild);
        sut.AddChild(child);

        sut.ToggleIncludeCommand.Execute(null);

        grandChild.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_expanded_then_pre_loaded_children_with_excluded_parent_remain_excluded()
    {
        var sut   = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        sut.ToggleExpandCommand.Execute(null);

        child.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_child_is_explicitly_excluded_then_it_starts_as_excluded_under_included_parent()
    {
        var sut   = BuildNode(FolderSyncState.Included, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        child.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_parent_is_included_and_child_is_toggled_excluded_then_parent_becomes_partial()
    {
        var sut   = BuildNode(FolderSyncState.Included, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Included);
        sut.AddChild(child);

        child.ToggleIncludeCommand.Execute(null);

        sut.SyncState.ShouldBe(FolderSyncState.Partial);
    }

    [Fact]
    public void when_all_children_included_and_parent_had_included_inherited_state_then_parent_is_included()
    {
        var sut   = BuildNode(FolderSyncState.Included, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Included);
        sut.AddChild(child);

        child.ToggleIncludeCommand.Execute(null);
        child.ToggleIncludeCommand.Execute(null);

        sut.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_all_children_excluded_and_parent_had_excluded_inherited_state_then_parent_is_excluded()
    {
        var sut   = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        sut.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_child_included_under_excluded_parent_then_parent_becomes_partial()
    {
        var sut   = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        child.ToggleIncludeCommand.Execute(null);

        sut.SyncState.ShouldBe(FolderSyncState.Partial);
    }

    [Fact]
    public void when_included_parent_is_toggled_to_excluded_then_children_cascade_to_excluded()
    {
        var sut   = BuildNode(FolderSyncState.Included, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Included);
        sut.AddChild(child);

        sut.ToggleIncludeCommand.Execute(null);

        child.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void when_excluded_parent_is_toggled_to_included_then_children_become_included()
    {
        var sut   = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        sut.ToggleIncludeCommand.Execute(null);

        child.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_toggled_then_include_toggled_event_is_raised()
    {
        var sut = BuildNode(FolderSyncState.Excluded);
        FolderTreeNodeViewModel? raised = null;
        sut.IncludeToggled += (_, node) => raised = node;

        sut.ToggleIncludeCommand.Execute(null);

        raised.ShouldNotBeNull();
        raised!.Id.ShouldBe(FolderId);
    }

    [Fact]
    public void when_toggled_then_child_state_changed_event_is_raised()
    {
        var sut = BuildNode(FolderSyncState.Excluded);
        FolderTreeNodeViewModel? raised = null;
        sut.ChildStateChanged += (_, node) => raised = node;

        sut.ToggleIncludeCommand.Execute(null);

        raised.ShouldNotBeNull();
    }

    [Fact]
    public void when_grandchild_state_changes_then_grandparent_receives_partial_state_via_propagation()
    {
        var grandParent = BuildNode(FolderSyncState.Included, hasChildren: true);
        var child       = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Included);
        var grandChild  = BuildChildNode(GrandChildId, "Sub", ChildId, FolderSyncState.Included);
        child.AddChild(grandChild);
        grandParent.AddChild(child);

        grandChild.ToggleIncludeCommand.Execute(null);

        grandParent.SyncState.ShouldBe(FolderSyncState.Partial);
    }

    [Fact]
    public void when_node_has_no_children_then_toggle_expand_does_nothing()
    {
        var sut = BuildNode(hasChildren: false);

        sut.ToggleExpandCommand.Execute(null);

        sut.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void when_node_has_children_then_toggle_expand_sets_expanded()
    {
        var sut   = BuildNode(hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId);
        sut.AddChild(child);

        sut.ToggleExpandCommand.Execute(null);

        sut.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void when_expanded_and_toggled_again_then_collapses()
    {
        var sut   = BuildNode(hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId);
        sut.AddChild(child);
        sut.ToggleExpandCommand.Execute(null);

        sut.ToggleExpandCommand.Execute(null);

        sut.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void when_add_child_called_then_child_include_toggled_propagates_to_parent_subscribers()
    {
        var sut   = BuildNode(FolderSyncState.Excluded, hasChildren: true);
        var child = BuildChildNode(ChildId, ChildName, FolderId, FolderSyncState.Excluded);
        sut.AddChild(child);

        FolderTreeNodeViewModel? propagated = null;
        sut.IncludeToggled += (_, node) => propagated = node;

        child.ToggleIncludeCommand.Execute(null);

        propagated.ShouldNotBeNull();
        propagated!.Id.ShouldBe(ChildId);
    }
}
