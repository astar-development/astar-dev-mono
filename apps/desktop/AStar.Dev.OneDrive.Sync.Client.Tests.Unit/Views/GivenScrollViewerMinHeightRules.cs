using Avalonia.Controls;
using Avalonia.VisualTree;
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

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "AccountsView ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_activity_view_log_list_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new ActivityView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "ActivityView log-list ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_activity_view_conflict_list_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new ActivityView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.Count.ShouldBeGreaterThanOrEqualTo(2, "ActivityView must have at least two vertically-scrollable ScrollViewers (log list and conflict list)");
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "ActivityView conflict-list ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_dashboard_view_account_sections_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new DashboardView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "DashboardView ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_files_view_folder_tree_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new FilesView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "FilesView folder-tree ScrollViewer must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_settings_view_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new SettingsView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "SettingsView ScrollViewer must declare MinHeight=0");
    }
}
