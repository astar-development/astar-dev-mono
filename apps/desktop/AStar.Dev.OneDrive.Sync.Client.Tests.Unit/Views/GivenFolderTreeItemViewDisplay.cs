using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

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
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

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
        string folderName = "My Documents";
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
        int depth = 3;
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

    [AvaloniaFact]
    public void when_folder_is_loading_children_then_progress_bar_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsLoadingChildren = true;

        var sut = CreateViewWithViewModel(viewModel);

        var progressBar = sut.GetLogicalDescendants().OfType<ProgressBar>().FirstOrDefault();
        progressBar.ShouldNotBeNull();
        progressBar.IsVisible.ShouldBeTrue("Loading indicator should be visible while children are loading");
    }

    [AvaloniaFact]
    public void when_folder_is_not_loading_children_then_progress_bar_is_hidden()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsLoadingChildren = false;

        var sut = CreateViewWithViewModel(viewModel);

        var progressBar = sut.GetLogicalDescendants().OfType<ProgressBar>().FirstOrDefault();
        progressBar.ShouldNotBeNull();
        progressBar.IsVisible.ShouldBeFalse("Loading indicator should be hidden when no children are loading");
    }

    [AvaloniaFact]
    public void when_folder_is_collapsed_then_children_items_control_is_hidden()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsExpanded = false;

        var sut = CreateViewWithViewModel(viewModel);

        var itemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault();
        itemsControl.ShouldNotBeNull();
        itemsControl.IsVisible.ShouldBeFalse("Children list should be hidden when the node is collapsed");
    }

    [AvaloniaFact]
    public void when_folder_is_expanded_then_children_items_control_is_visible_and_bound_to_children()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.Children.Add(CreateFolderTreeNode("Child", 1, false, FolderSyncState.Included));
        viewModel.IsExpanded = true;

        var sut = CreateViewWithViewModel(viewModel);

        var itemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault();
        itemsControl.ShouldNotBeNull();
        itemsControl.IsVisible.ShouldBeTrue("Children list should be visible when the node is expanded");
        itemsControl.ItemsSource.ShouldBe(viewModel.Children);
    }

    [AvaloniaFact]
    public void when_expansion_toggles_after_render_then_expander_glyph_updates()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, true, FolderSyncState.Included);
        viewModel.IsExpanded = false;
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.IsExpanded = true;

        var expanderText = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "▾");
        expanderText.ShouldNotBeNull("Expander glyph should update to down arrow (▾) when expanded after render");
    }

    [AvaloniaFact]
    public void when_sync_state_changes_after_render_then_toggle_label_updates()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);
        var sut = CreateViewWithViewModel(viewModel);
        var toggleLabel = sut.GetLogicalDescendants()
            .OfType<Button>()
            .Where(btn => btn.BorderThickness.Top > 0)
            .Select(btn => btn.Content)
            .OfType<TextBlock>()
            .First();
        toggleLabel.Text.ShouldBe("Files.Exclude");

        viewModel.SyncState = FolderSyncState.Excluded;

        toggleLabel.Text.ShouldBe("Files.Include", "Toggle label should flip to Include when the folder becomes excluded");
    }

    [AvaloniaTheory]
    [InlineData(FolderSyncState.Excluded, "Files.FolderStatus.Excluded")]
    [InlineData(FolderSyncState.Partial, "Files.FolderStatus.Partial")]
    [InlineData(FolderSyncState.Synced, "Files.FolderStatus.Synced")]
    [InlineData(FolderSyncState.Conflict, "Files.FolderStatus.Conflict")]
    [InlineData(FolderSyncState.Error, "Files.FolderStatus.Error")]
    public void when_sync_state_is_set_then_status_badge_displays_state_text(FolderSyncState syncState, string expectedBadgeText)
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, syncState);

        var sut = CreateViewWithViewModel(viewModel);

        var badgeText = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == expectedBadgeText);
        badgeText.ShouldNotBeNull($"Status badge should display '{expectedBadgeText}' for {syncState}");
    }

    [AvaloniaFact]
    public void when_folder_node_is_leaf_then_spacer_border_is_visible()
    {
        var viewModel = CreateFolderTreeNode("Test", 0, false, FolderSyncState.Included);

        var sut = CreateViewWithViewModel(viewModel);

        var spacer = sut.GetLogicalDescendants().OfType<Border>().FirstOrDefault(b => b.Width == 24 && b.IsVisible);
        spacer.ShouldNotBeNull("Spacer border should keep alignment when the node has no children");
    }
}
