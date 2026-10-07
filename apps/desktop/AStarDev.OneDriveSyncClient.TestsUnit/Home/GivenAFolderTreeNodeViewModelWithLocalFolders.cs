using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Home;

public sealed class GivenAFolderTreeNodeViewModelWithLocalFolders
{
    private const string DriveIdString = "drive-1";
    private const string RootFolderId = "folder-root";
    private const string RootFolderName = "Documents";
    private const string RootPath = "/Documents";

    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("token-abc");

    private static IReadOnlyList<string> NoLocalFolders(string remotePath) => [];

    private static IGraphService BuildGraphServiceWithRemoteChildren(params string[] remoteChildNames)
    {
        var graphService = Substitute.For<IGraphService>();
        var folders = remoteChildNames.Select(name => new DriveFolder($"remote-{name}", name, RootFolderId)).ToList();

        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), new DriveId(DriveIdString), RootFolderId, Arg.Any<CancellationToken>())
            .Returns(new Ok<List<DriveFolder>, string>(folders));

        return graphService;
    }

    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => $"{x.ArgAt<string>(0)}:{x.ArgAt<object[]>(1)[0]}");

        return loc;
    }

    private static FolderTreeNodeViewModel BuildRootVm(IGraphService graphService, Func<string, IReadOnlyList<string>> localFolderLister, Func<string, FolderSyncState?>? ruleStateResolver = null, Option<string>? remoteId = null, string path = RootPath, FolderSyncState syncState = FolderSyncState.Included)
    {
        var node = new FolderTreeNode(remoteId ?? Option.Some(RootFolderId), RootFolderName, Option.None<string>(), "account-1", path, syncState, HasChildren: true);

        return new FolderTreeNodeViewModel(node, graphService, TokenFactory, new DriveId(DriveIdString), ruleStateResolver ?? (_ => null), localFolderLister, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), BuildLocalizationService());
    }

    [Fact]
    public async Task when_a_local_folder_has_no_remote_match_then_it_is_added_as_a_local_only_child()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren("Work"), path => path == RootPath ? ["Drafts"] : []);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        var child = sut.Children.Single(node => node.Name == "Drafts");
        child.IsLocalOnly.ShouldBeTrue();
        child.RemoteId.ShouldBe(Option.None<string>());
        child.RemotePath.ShouldBe("/Documents/Drafts");
    }

    [Fact]
    public async Task when_children_are_merged_then_they_are_ordered_by_name()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren("Work"), path => path == RootPath ? ["Drafts"] : []);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children.Select(node => node.Name).ShouldBe(["Drafts", "Work"]);
    }

    [Fact]
    public async Task when_a_local_only_child_has_no_rule_then_it_is_included()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), path => path == RootPath ? ["Drafts"] : [], syncState: FolderSyncState.Excluded);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children.Single().SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_a_local_only_child_has_an_exclude_rule_then_it_is_excluded()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), path => path == RootPath ? ["Drafts"] : [], path => path == "/Documents/Drafts" ? FolderSyncState.Excluded : null);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children.Single().SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_a_local_folder_matches_a_remote_folder_then_a_single_child_that_is_not_local_only_is_shown()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren("Work"), path => path == RootPath ? ["Work"] : []);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        var child = sut.Children.ShouldHaveSingleItem();
        child.IsLocalOnly.ShouldBeFalse();
        child.RemoteId.ShouldBe(Option.Some("remote-Work"));
    }

    [Fact]
    public async Task when_a_local_only_node_is_expanded_then_remote_children_are_not_requested()
    {
        var graphService = BuildGraphServiceWithRemoteChildren();
        var sut = BuildRootVm(graphService, path => path == "/Drafts" ? ["Notes"] : [], remoteId: Option.None<string>(), path: "/Drafts");

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        await graphService.DidNotReceive().GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        sut.Children.Single().Name.ShouldBe("Notes");
    }

    [Fact]
    public async Task when_a_local_only_node_is_expanded_then_its_local_only_children_have_no_remote_parent()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), path => path == "/Drafts" ? ["Notes"] : [], remoteId: Option.None<string>(), path: "/Drafts");

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.Children.Single().ParentId.ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_local_only_node_has_no_local_children_then_it_is_marked_as_having_no_children()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), NoLocalFolders, remoteId: Option.None<string>(), path: "/Drafts");

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        sut.HasChildren.ShouldBeFalse();
    }

    [Fact]
    public void when_a_node_has_a_remote_id_then_it_is_not_local_only()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), NoLocalFolders);

        sut.IsLocalOnly.ShouldBeFalse();
    }

    [Fact]
    public void when_a_node_has_no_remote_id_then_it_is_local_only_and_shows_the_local_only_badge()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), NoLocalFolders, remoteId: Option.None<string>());

        sut.IsLocalOnly.ShouldBeTrue();
        sut.LocalOnlyBadgeText.ShouldBe("Files.FolderStatus.LocalOnly");
    }

    [Fact]
    public async Task when_a_local_only_child_has_no_rule_then_it_is_reported_as_discovered()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), path => path == RootPath ? ["Drafts"] : []);
        var discovered = new List<string>();
        sut.LocalOnlyFolderDiscovered += (_, node) => discovered.Add(node.RemotePath);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        discovered.ShouldBe(["/Documents/Drafts"]);
    }

    [Fact]
    public async Task when_a_local_only_child_already_has_a_rule_then_it_is_not_reported_as_discovered()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren(), path => path == RootPath ? ["Drafts"] : [], _ => FolderSyncState.Included);
        var discovered = new List<string>();
        sut.LocalOnlyFolderDiscovered += (_, node) => discovered.Add(node.RemotePath);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        discovered.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_remote_child_has_no_rule_then_it_is_not_reported_as_discovered()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren("Work"), NoLocalFolders);
        var discovered = new List<string>();
        sut.LocalOnlyFolderDiscovered += (_, node) => discovered.Add(node.RemotePath);

        await sut.ToggleExpandCommand.ExecuteAsync(null);

        discovered.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_grandchild_local_only_folder_is_discovered_then_the_root_is_notified()
    {
        var sut = BuildRootVm(BuildGraphServiceWithRemoteChildren("Work"), path => path == "/Documents/Work" ? ["Notes"] : []);
        var discovered = new List<string>();
        sut.LocalOnlyFolderDiscovered += (_, node) => discovered.Add(node.RemotePath);
        await sut.ToggleExpandCommand.ExecuteAsync(null);

        await sut.Children.Single().ToggleExpandCommand.ExecuteAsync(null);

        discovered.ShouldBe(["/Documents/Work/Notes"]);
    }
}
