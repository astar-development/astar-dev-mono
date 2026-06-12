using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

/// <summary>
/// Option 2: Logical Tree Content Tests for FolderTreeItemView.
/// Instantiate the view with a FolderTreeNodeViewModel, inspect the logical tree
/// for specific elements, visibility states, and control hierarchy.
/// </summary>
public sealed class GivenFolderTreeItemViewDisplay
{
    private static FolderTreeItemView CreateViewWithViewModel(FolderTreeNodeViewModel viewModel)
    {
        var view = new FolderTreeItemView { DataContext = viewModel };
        view.Measure(new(500, 32));
        view.Arrange(new(0, 0, 500, 32));
        return view;
    }

    private static FolderTreeNodeViewModel CreateFolderTreeNode(
        string name,
        int depth,
        bool hasChildren,
        FolderSyncState syncState)
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns("Label");
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Label");

        var logger = Substitute.For<ILogger<FolderTreeNodeViewModel>>();
        var graphService = Substitute.For<IGraphService>();
        var tokenFactory = Substitute.For<Func<CancellationToken, Task<string>>>();

        var node = new FolderTreeNode(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            ParentId: Option.None<string>(),
            AccountId: "test-account",
            RemotePath: $"/root/{name}",
            SyncState: syncState,
            HasChildren: hasChildren
        );

        return new FolderTreeNodeViewModel(
            node,
            graphService,
            tokenFactory,
            new DriveId("test-drive"),
            _ => syncState,
            logger,
            localization,
            depth
        );
    }

    [AvaloniaFact]
    public void when_folder_node_has_no_children_then_expander_button_is_hidden()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);
        var sut = CreateViewWithViewModel(viewModel);

        var expanderButtons = sut.GetLogicalDescendants()
            .OfType<Button>()
            .Where(btn => btn.Width == 24 && btn.Height == 24)
            .ToList();

        var hiddenExpanderButton = expanderButtons.FirstOrDefault(btn => !btn.IsVisible);
        hiddenExpanderButton.ShouldNotBeNull("Expander button should be hidden when HasChildren is false");
    }

    [AvaloniaFact]
    public void when_folder_node_has_children_then_expander_button_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        var sut = CreateViewWithViewModel(viewModel);

        var expanderButton = sut.GetLogicalDescendants()
            .OfType<Button>()
            .FirstOrDefault(btn => btn.IsVisible && btn.Width == 24 && btn.Height == 24);

        expanderButton.ShouldNotBeNull("Expander button should be visible when HasChildren is true");
    }

    [AvaloniaFact]
    public void when_folder_tree_node_is_collapsed_then_expander_shows_right_arrow()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsExpanded = false;
        var sut = CreateViewWithViewModel(viewModel);

        var expanderText = sut.GetLogicalDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == "▸");

        expanderText.ShouldNotBeNull("Expander should display right arrow (▸) when collapsed");
    }

    [AvaloniaFact]
    public void when_folder_tree_node_is_expanded_then_expander_shows_down_arrow()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsExpanded = true;
        var sut = CreateViewWithViewModel(viewModel);

        var expanderText = sut.GetLogicalDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == "▾");

        expanderText.ShouldNotBeNull("Expander should display down arrow (▾) when expanded");
    }

    [AvaloniaFact]
    public void when_folder_name_is_set_then_name_text_block_displays_folder_name()
    {
        var folderName = "My Documents";
        var viewModel = CreateFolderTreeNode(folderName, 0, false, FolderSyncState.Included);
        var sut = CreateViewWithViewModel(viewModel);

        var nameTextBlock = sut.GetLogicalDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == folderName);

        nameTextBlock.ShouldNotBeNull($"Folder name text block should display '{folderName}'");
    }

    [AvaloniaFact]
    public void when_sync_state_is_included_then_status_badge_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);
        var sut = CreateViewWithViewModel(viewModel);

        var statusBadges = sut.GetLogicalDescendants()
            .OfType<Border>()
            .Where(b => b.IsVisible && b.Child is TextBlock)
            .ToList();

        statusBadges.ShouldNotBeEmpty("Status badge should be present for Included state");
    }

    [AvaloniaFact]
    public void when_sync_state_is_syncing_then_status_badge_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Syncing);
        var sut = CreateViewWithViewModel(viewModel);

        var statusBadges = sut.GetLogicalDescendants()
            .OfType<Border>()
            .Where(b => b.IsVisible && b.Child is TextBlock)
            .ToList();

        statusBadges.Count.ShouldBeGreaterThanOrEqualTo(1, "Status badge should be visible for Syncing state");
    }

    [AvaloniaFact]
    public void when_folder_is_included_then_toggle_button_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);
        viewModel.IsIncluded.ShouldBeTrue();
        var sut = CreateViewWithViewModel(viewModel);

        var toggleButtons = sut.GetLogicalDescendants()
            .OfType<Button>()
            .Where(btn => btn.BorderThickness.Top > 0 && btn.IsVisible)
            .ToList();

        toggleButtons.ShouldNotBeEmpty("Include/Exclude toggle button should be visible");
    }

    [AvaloniaFact]
    public void when_folder_is_excluded_then_toggle_button_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Excluded);
        viewModel.IsExcluded.ShouldBeTrue();
        var sut = CreateViewWithViewModel(viewModel);

        var toggleButtons = sut.GetLogicalDescendants()
            .OfType<Button>()
            .Where(btn => btn.BorderThickness.Top > 0 && btn.IsVisible)
            .ToList();

        toggleButtons.ShouldNotBeEmpty("Include/Exclude toggle button should be visible for excluded folders");
    }

    [AvaloniaFact]
    public void when_folder_depth_is_set_then_depth_property_reflects_hierarchy_level()
    {
        var depth = 3;
        var viewModel = CreateFolderTreeNode("Test", depth, false, FolderSyncState.Included);

        viewModel.Depth.ShouldBe(depth, "Depth property should be set correctly for tree hierarchy");
    }

    [AvaloniaFact]
    public void when_folder_tree_item_is_rendered_then_grid_with_seven_columns_is_present()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);
        var sut = CreateViewWithViewModel(viewModel);

        var mainGrid = sut.GetLogicalDescendants()
            .OfType<Grid>()
            .FirstOrDefault(g => g.ColumnDefinitions.Count == 7);

        mainGrid.ShouldNotBeNull("Main grid with 7 columns should be present");
    }

    [AvaloniaFact]
    public void when_folder_has_children_collection_then_children_are_accessible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        var child1 = CreateFolderTreeNode("Child 1", 1, false, FolderSyncState.Included);
        var child2 = CreateFolderTreeNode("Child 2", 1, false, FolderSyncState.Included);

        viewModel.Children.Add(child1);
        viewModel.Children.Add(child2);

        viewModel.Children.Count.ShouldBe(2, "Children collection should contain added items");
    }

    [AvaloniaFact]
    public void when_folder_sync_state_changes_then_is_included_property_updates()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);
        viewModel.IsIncluded.ShouldBeTrue();

        viewModel.SyncState = FolderSyncState.Excluded;

        viewModel.SyncState.ShouldBe(FolderSyncState.Excluded);
        viewModel.IsExcluded.ShouldBeTrue("IsExcluded property should update when SyncState changes");
    }

    [AvaloniaFact]
    public void when_folder_is_loading_children_then_loading_state_is_settable()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsLoadingChildren = false;

        viewModel.IsLoadingChildren.ShouldBeFalse();

        viewModel.IsLoadingChildren = true;

        viewModel.IsLoadingChildren.ShouldBeTrue("IsLoadingChildren property should be settable");
    }
}
