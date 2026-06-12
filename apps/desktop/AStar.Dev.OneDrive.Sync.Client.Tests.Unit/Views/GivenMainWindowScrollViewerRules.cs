using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenMainWindowScrollViewerRules
{
    [AvaloniaFact]
    public void when_account_list_scroll_viewer_is_inspected_then_it_is_not_docked_in_a_dock_panel()
    {
        var sut = new MainWindow();

        var dockPanelHostedScrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(sv => sv.GetLogicalParent() is DockPanel);

        dockPanelHostedScrollViewer.ShouldBeNull("account-list ScrollViewer must not be a direct child of a DockPanel; it must sit in a Grid star row");
    }

    [AvaloniaFact]
    public void when_account_list_scroll_viewer_is_inspected_then_min_height_is_zero()
    {
        var sut = new MainWindow();

        var scrollViewers = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "every vertically-scrollable ScrollViewer in MainWindow must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_account_list_scroll_viewer_is_inspected_then_it_sits_in_a_star_grid_row()
    {
        var sut = new MainWindow();

        var accountListScrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .First(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto);
        var hostGrid = accountListScrollViewer.GetLogicalParent() as Grid;

        hostGrid.ShouldNotBeNull("account-list ScrollViewer must be a direct child of a Grid");
        hostGrid.RowDefinitions[Grid.GetRow(accountListScrollViewer)].Height.IsStar.ShouldBeTrue("account-list ScrollViewer must sit in a star row");
    }
}
