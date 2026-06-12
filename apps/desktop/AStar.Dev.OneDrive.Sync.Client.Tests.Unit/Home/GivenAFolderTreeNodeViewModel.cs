using System.Globalization;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFolderTreeNodeViewModel
{
    private const string DriveIdString   = "drive-1";
    private const string RootFolderId   = "folder-root";
    private const string RootFolderName = "Documents";
    private const string ChildFolderId  = "folder-child";
    private const string ChildName      = "Work";
    private const string GrandChildId   = "folder-grandchild";
    private const string GrandChildName = "Projects";

    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("token-abc");

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

        int eventCount = 0;
        sut.IncludeToggled += (_, _) => eventCount++;

        sut.ToggleIncludeCommand.Execute(null);

        eventCount.ShouldBe(1);
    }

    [Fact]
    public void when_is_included_then_toggle_label_returns_files_exclude_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included);

        sut.ToggleLabel.ShouldBe("Files.Exclude");
    }

    [Fact]
    public void when_is_excluded_then_toggle_label_returns_files_include_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Excluded);

        sut.ToggleLabel.ShouldBe("Files.Include");
    }

    [Fact]
    public void when_sync_state_changes_to_excluded_then_property_changed_fires_for_toggle_label()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        sut.SyncState = FolderSyncState.Excluded;

        firedProperties.ShouldContain(nameof(sut.ToggleLabel));
    }

    [Fact]
    public void when_sync_state_changes_to_included_then_property_changed_fires_for_toggle_label()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Excluded);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        sut.SyncState = FolderSyncState.Included;

        firedProperties.ShouldContain(nameof(sut.ToggleLabel));
    }

    [Fact]
    public void when_culture_changed_then_toggle_label_is_refreshed()
    {
        var loc = BuildLocalizationService();
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included, loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("Files.Exclude");
    }

    [Fact]
    public void when_culture_changed_then_property_changed_fires_for_toggle_label()
    {
        var loc = BuildLocalizationService();
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included, loc);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        firedProperties.ShouldContain(nameof(sut.ToggleLabel));
    }

    [Fact]
    public void when_is_included_then_toggle_tooltip_includes_folder_name()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included);

        sut.ToggleTooltip.ShouldBe($"Files.Exclude.Tooltip:{RootFolderName}");
    }

    [Fact]
    public void when_is_excluded_then_toggle_tooltip_includes_folder_name()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Excluded);

        sut.ToggleTooltip.ShouldBe($"Files.Include.Tooltip:{RootFolderName}");
    }

    [Fact]
    public void when_sync_state_changes_to_excluded_then_property_changed_fires_for_toggle_tooltip()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        sut.SyncState = FolderSyncState.Excluded;

        firedProperties.ShouldContain(nameof(sut.ToggleTooltip));
    }

    [Fact]
    public void when_sync_state_changes_to_included_then_property_changed_fires_for_toggle_tooltip()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Excluded);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        sut.SyncState = FolderSyncState.Included;

        firedProperties.ShouldContain(nameof(sut.ToggleTooltip));
    }

    [Fact]
    public void when_culture_changed_then_toggle_tooltip_is_refreshed_with_folder_name()
    {
        var loc = BuildLocalizationService();
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included, loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("Files.Exclude.Tooltip", Arg.Any<object[]>());
    }

    [Fact]
    public void when_culture_changed_then_property_changed_fires_for_toggle_tooltip()
    {
        var loc = BuildLocalizationService();
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included, loc);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        firedProperties.ShouldContain(nameof(sut.ToggleTooltip));
    }

    [Fact]
    public void when_sync_state_is_included_then_status_badge_text_resolves_included_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Included);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Included");
    }

    [Fact]
    public void when_sync_state_is_synced_then_status_badge_text_resolves_synced_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Synced);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Synced");
    }

    [Fact]
    public void when_sync_state_is_syncing_then_status_badge_text_resolves_syncing_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Syncing);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Syncing");
    }

    [Fact]
    public void when_sync_state_is_partial_then_status_badge_text_resolves_partial_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Partial);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Partial");
    }

    [Fact]
    public void when_sync_state_is_conflict_then_status_badge_text_resolves_conflict_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Conflict);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Conflict");
    }

    [Fact]
    public void when_sync_state_is_error_then_status_badge_text_resolves_error_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Error);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Error");
    }

    [Fact]
    public void when_sync_state_is_excluded_then_status_badge_text_resolves_excluded_key()
    {
        var sut = BuildRootVm(Substitute.For<IGraphService>(), FolderSyncState.Excluded);

        sut.StatusBadgeText.ShouldBe("Files.FolderStatus.Excluded");
    }

    private static IGraphService BuildGraphServiceWithChild()
    {
        var graphService = Substitute.For<IGraphService>();

        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), new DriveId(DriveIdString), RootFolderId, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(ChildFolderId, ChildName, RootFolderId)]));

        return graphService;
    }

    private static IGraphService BuildGraphServiceWithGrandchild()
    {
        var graphService = Substitute.For<IGraphService>();

        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), new DriveId(DriveIdString), RootFolderId, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(ChildFolderId, ChildName, RootFolderId)]));

        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), new DriveId(DriveIdString), ChildFolderId, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(GrandChildId, GrandChildName, ChildFolderId)]));

        return graphService;
    }

    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => $"{x.ArgAt<string>(0)}:{x.ArgAt<object[]>(1)[0]}");

        return loc;
    }

    private static FolderTreeNodeViewModel BuildIncludedRootVm(IGraphService graphService)
        => BuildRootVm(graphService, FolderSyncState.Included);

    private static FolderTreeNodeViewModel BuildExcludedRootVm(IGraphService graphService)
        => BuildRootVm(graphService, FolderSyncState.Excluded);

    private static FolderTreeNodeViewModel BuildRootVm(IGraphService graphService, FolderSyncState syncState)
        => BuildRootVm(graphService, syncState, BuildLocalizationService());

    private static FolderTreeNodeViewModel BuildRootVm(IGraphService graphService, FolderSyncState syncState, ILocalizationService loc)
    {
        var node = new FolderTreeNode(
            Id:          RootFolderId,
            Name:        RootFolderName,
            ParentId:    Option.None<string>(),
            AccountId:   "account-1",
            RemotePath:  $"/{RootFolderName}",
            SyncState:   syncState,
            HasChildren: true);

        return new FolderTreeNodeViewModel(node, graphService, TokenFactory, new DriveId(DriveIdString), _ => null, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), loc);
    }
}
