using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenScrollViewerMinHeightRules
{
    [AvaloniaFact]
    public void when_accounts_view_account_list_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new AccountsView();

        var scrollViewers = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "AccountsView ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_activity_view_log_list_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new ActivityView();

        var scrollViewers = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "ActivityView log-list ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_activity_view_conflict_list_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new ActivityView();

        var scrollViewers = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.Count.ShouldBeGreaterThanOrEqualTo(2, "ActivityView must have at least two vertically-scrollable ScrollViewers (log list and conflict list)");
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "ActivityView conflict-list ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_dashboard_view_account_sections_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new DashboardView();

        var scrollViewers = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "DashboardView ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_files_view_folder_tree_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new FilesView();
        var activeTabTemplate = sut.GetLogicalDescendants().OfType<ContentControl>().SelectMany(contentControl => contentControl.DataTemplates).Single();

        var activeTabContent = activeTabTemplate.Build(null);
        var scrollViewers = activeTabContent!.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "FilesView folder-tree ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_settings_view_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new SettingsView();

        var scrollViewers = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "SettingsView ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_settings_view_scroll_viewer_is_inspected_then_it_is_not_the_sole_child_of_a_single_star_row_grid()
    {
        var sut = new SettingsView();

        var scrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .First(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto);

        var parent = scrollViewer.GetLogicalParent();
        var isSingleStarRowGrid = parent is Grid g && g.RowDefinitions.Count == 1 && g.RowDefinitions[0].Height.IsStar;
        isSingleStarRowGrid.ShouldBeFalse("SettingsView ScrollViewer must not be the sole child of a single-star-row Grid — the star row cannot bind the viewport when the Grid is measured with infinite height");
    }

    [AvaloniaFact]
    public void when_settings_view_scroll_viewer_is_inspected_then_it_is_in_a_star_row_of_a_multi_row_grid()
    {
        var sut = new SettingsView();

        var scrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .First(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto);

        var parent = scrollViewer.GetLogicalParent();
        var rowIndex = (int)scrollViewer.GetValue(Grid.RowProperty);
        var isInStarRowOfMultiRowGrid = parent is Grid g && g.RowDefinitions.Count > 1 && g.RowDefinitions[rowIndex].Height.IsStar;
        isInStarRowOfMultiRowGrid.ShouldBeTrue("SettingsView ScrollViewer must be in a * row of a multi-row Grid — a direct UserControl child or lone-star-row Grid cannot bound the viewport height during measure; only star allocation in a Grid with real Auto content above provides a finite available height to the ScrollViewer");
    }
}
