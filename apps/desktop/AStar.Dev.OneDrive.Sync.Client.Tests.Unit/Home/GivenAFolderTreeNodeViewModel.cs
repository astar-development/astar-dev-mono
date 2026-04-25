using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFolderTreeNodeViewModel
{
    private const string AccessToken    = "token-abc";
    private const string DriveId        = "drive-1";
    private const string RootFolderId   = "folder-root";
    private const string RootFolderName = "Documents";
    private const string ChildFolderId  = "folder-child";
    private const string ChildName      = "Work";
    private const string GrandChildId   = "folder-grandchild";
    private const string GrandChildName = "Projects";

    [Fact]
    public async Task when_included_parent_is_toggled_excluded_then_loaded_children_are_also_excluded()
    {
        var graphService = BuildGraphServiceWithChild();
        var sut = BuildIncludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.ToggleIncludeCommand.Execute(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_excluded_parent_is_toggled_included_then_loaded_children_are_also_included()
    {
        var graphService = BuildGraphServiceWithChild();
        var sut = BuildExcludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.ToggleIncludeCommand.Execute(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_included_parent_is_toggled_excluded_then_grandchildren_are_also_excluded()
    {
        var graphService = BuildGraphServiceWithGrandchild();
        var sut = BuildIncludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);
        await sut.Children[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.ToggleIncludeCommand.Execute(null);

        sut.Children[0].Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_excluded_parent_is_toggled_included_then_grandchildren_are_also_included()
    {
        var graphService = BuildGraphServiceWithGrandchild();
        var sut = BuildExcludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);
        await sut.Children[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.ToggleIncludeCommand.Execute(null);

        sut.Children[0].Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_children_are_loaded_under_included_parent_they_inherit_included_state()
    {
        var graphService = BuildGraphServiceWithChild();
        var sut = BuildIncludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_children_are_loaded_under_excluded_parent_they_inherit_excluded_state()
    {
        var graphService = BuildGraphServiceWithChild();
        var sut = BuildExcludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_parent_is_toggled_then_include_toggled_event_fires_once()
    {
        var graphService = BuildGraphServiceWithChild();
        var sut = BuildExcludedRootVm(graphService);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        var eventCount = 0;
        sut.IncludeToggled += (_, _) => eventCount++;

        sut.ToggleIncludeCommand.Execute(null);

        eventCount.ShouldBe(1);
    }

    private static IGraphService BuildGraphServiceWithChild()
    {
        var graphService = Substitute.For<IGraphService>();

        graphService.GetChildFoldersAsync(AccessToken, DriveId, RootFolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildFolderId, ChildName, RootFolderId)]);

        return graphService;
    }

    private static IGraphService BuildGraphServiceWithGrandchild()
    {
        var graphService = Substitute.For<IGraphService>();

        graphService.GetChildFoldersAsync(AccessToken, DriveId, RootFolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildFolderId, ChildName, RootFolderId)]);

        graphService.GetChildFoldersAsync(AccessToken, DriveId, ChildFolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(GrandChildId, GrandChildName, ChildFolderId)]);

        return graphService;
    }

    private static FolderTreeNodeViewModel BuildIncludedRootVm(IGraphService graphService)
        => BuildRootVm(graphService, FolderSyncState.Included);

    private static FolderTreeNodeViewModel BuildExcludedRootVm(IGraphService graphService)
        => BuildRootVm(graphService, FolderSyncState.Excluded);

    private static FolderTreeNodeViewModel BuildRootVm(IGraphService graphService, FolderSyncState syncState)
    {
        var node = new FolderTreeNode(
            Id:          RootFolderId,
            Name:        RootFolderName,
            ParentId:    null,
            AccountId:   "account-1",
            RemotePath:  $"/{RootFolderName}",
            SyncState:   syncState,
            HasChildren: true);

        return new FolderTreeNodeViewModel(node, graphService, AccessToken, DriveId);
    }
}
