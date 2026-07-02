using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFolderTreeNodeViewModelWithARuleStateResolver
{
    private const string DriveIdString   = "drive-1";
    private const string RootFolderId   = "folder-root";
    private const string RootFolderName = "Documents";
    private const string ChildFolderId  = "folder-child";
    private const string ChildName      = "Work";

    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("token-abc");

    [Fact]
    public async Task when_child_has_persisted_exclude_rule_under_included_parent_then_child_sync_state_is_excluded()
    {
        var graphService = BuildGraphServiceWithChild();
        string expectedChildPath = $"/{RootFolderName}/{ChildName}";
        Func<string, FolderSyncState?> resolver = path => path.Equals(expectedChildPath, StringComparison.OrdinalIgnoreCase) ? FolderSyncState.Excluded : null;
        var sut = BuildRootVm(graphService, FolderSyncState.Included, resolver);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_child_has_persisted_include_rule_under_excluded_parent_then_child_sync_state_is_included()
    {
        var graphService = BuildGraphServiceWithChild();
        string expectedChildPath = $"/{RootFolderName}/{ChildName}";
        Func<string, FolderSyncState?> resolver = path => path.Equals(expectedChildPath, StringComparison.OrdinalIgnoreCase) ? FolderSyncState.Included : null;
        var sut = BuildRootVm(graphService, FolderSyncState.Excluded, resolver);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_child_has_no_persisted_rule_then_child_inherits_included_parent_state()
    {
        var graphService = BuildGraphServiceWithChild();
        Func<string, FolderSyncState?> resolver = _ => null;
        var sut = BuildRootVm(graphService, FolderSyncState.Included, resolver);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_child_has_no_persisted_rule_then_child_inherits_excluded_parent_state()
    {
        var graphService = BuildGraphServiceWithChild();
        Func<string, FolderSyncState?> resolver = _ => null;
        var sut = BuildRootVm(graphService, FolderSyncState.Excluded, resolver);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    private static IGraphService BuildGraphServiceWithChild()
    {
        var graphService = Substitute.For<IGraphService>();

        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), new DriveId(DriveIdString), RootFolderId, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(ChildFolderId, ChildName, RootFolderId)]));

        return graphService;
    }

    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => $"{x.ArgAt<string>(0)}:{x.ArgAt<object[]>(1)[0]}");

        return loc;
    }

    private static FolderTreeNodeViewModel BuildRootVm(IGraphService graphService, FolderSyncState syncState, Func<string, FolderSyncState?> ruleStateResolver)
    {
        var node = new FolderTreeNode(
            Id:          RootFolderId,
            Name:        RootFolderName,
            ParentId: Option.None<string>(),
            AccountId:   "account-1",
            RemotePath:  $"/{RootFolderName}",
            SyncState:   syncState,
            HasChildren: true);

        return new FolderTreeNodeViewModel(node, graphService, TokenFactory, new DriveId(DriveIdString), ruleStateResolver, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), BuildLocalizationService());
    }
}
