using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFolderTreeNodeViewModel
{
    private const string FolderId     = "folder-1";
    private const string FolderName   = "Documents";
    private const string ChildId      = "child-1";
    private const string ChildName    = "Work";
    private const string GrandChildId = "grandchild-1";
    private const string DriveId      = "drive-1";
    private const string AccessToken  = "token-abc";

    private readonly IGraphService _graphService = Substitute.For<IGraphService>();

    private FolderTreeNodeViewModel BuildNode(FolderSyncState syncState = FolderSyncState.Excluded, IReadOnlySet<OneDriveFolderId>? exclusions = null)
    {
        var node = new FolderTreeNode(FolderId, FolderName, null, string.Empty, syncState);

        return new FolderTreeNodeViewModel(node, _graphService, AccessToken, DriveId, exclusions ?? new HashSet<OneDriveFolderId>());
    }

    [Fact]
    public async Task when_toggled_to_included_then_sync_state_is_included()
    {
        var sut = BuildNode(FolderSyncState.Excluded);

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        sut.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_toggled_to_excluded_then_sync_state_is_excluded()
    {
        var sut = BuildNode(FolderSyncState.Included);

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        sut.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_included_then_children_are_deep_loaded_and_included()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Excluded);

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_included_then_grandchildren_are_deep_loaded_and_included()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, ChildId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(GrandChildId, "Sub", ChildId)]);

        var sut = BuildNode(FolderSyncState.Excluded);

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        sut.Children[0].Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_excluded_parent_expands_then_children_inherit_excluded_state()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Excluded);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_child_is_in_exclusion_set_then_it_starts_as_excluded_even_under_included_parent()
    {
        var explicitExclusions = new HashSet<OneDriveFolderId> { new(ChildId) };

        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Included, explicitExclusions);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_parent_is_included_and_child_is_toggled_excluded_then_parent_becomes_partial()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Included);
        await sut.ToggleExpandCommand.ExecuteAsync(null);

        await sut.Children[0].ToggleIncludeCommand.ExecuteAsync(null);

        sut.SyncState.ShouldBe(FolderSyncState.Partial);
    }

    [Fact]
    public async Task when_all_children_are_included_and_parent_had_included_inherited_state_then_parent_is_included()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Included);
        await sut.ToggleExpandCommand.ExecuteAsync(null);

        await sut.Children[0].ToggleIncludeCommand.ExecuteAsync(null);
        await sut.Children[0].ToggleIncludeCommand.ExecuteAsync(null);

        sut.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_all_children_excluded_and_parent_had_excluded_inherited_state_then_parent_is_excluded()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Excluded);
        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_child_included_under_excluded_parent_then_parent_becomes_partial()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Excluded);
        await sut.ToggleExpandCommand.ExecuteAsync(null);

        await sut.Children[0].ToggleIncludeCommand.ExecuteAsync(null);

        sut.SyncState.ShouldBe(FolderSyncState.Partial);
    }

    [Fact]
    public async Task when_included_parent_is_toggled_to_excluded_then_loaded_children_cascade_to_excluded()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Excluded);
        await sut.ToggleExpandCommand.ExecuteAsync(null);

        await sut.ToggleIncludeCommand.ExecuteAsync(null);
        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_excluded_parent_is_toggled_to_included_then_deep_loaded_children_are_included()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);

        var sut = BuildNode(FolderSyncState.Excluded);

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_toggled_then_include_toggled_event_is_raised()
    {
        var sut = BuildNode(FolderSyncState.Excluded);
        FolderTreeNodeViewModel? raised = null;
        sut.IncludeToggled += (_, node) => raised = node;

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        raised.ShouldNotBeNull();
        raised!.Id.ShouldBe(FolderId);
    }

    [Fact]
    public async Task when_toggled_then_child_state_changed_event_is_raised()
    {
        var sut = BuildNode(FolderSyncState.Excluded);
        FolderTreeNodeViewModel? raised = null;
        sut.ChildStateChanged += (_, node) => raised = node;

        await sut.ToggleIncludeCommand.ExecuteAsync(null);

        raised.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_child_state_changes_then_grandparent_receives_partial_state_via_propagation()
    {
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildId, ChildName, FolderId)]);
        _graphService.GetChildFoldersAsync(AccessToken, DriveId, ChildId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(GrandChildId, "Sub", ChildId)]);

        var grandParent = BuildNode(FolderSyncState.Included);
        await grandParent.ToggleExpandCommand.ExecuteAsync(null);

        var child = grandParent.Children[0];
        await child.ToggleExpandCommand.ExecuteAsync(null);

        await child.Children[0].ToggleIncludeCommand.ExecuteAsync(null);

        grandParent.SyncState.ShouldBe(FolderSyncState.Partial);
    }
}
