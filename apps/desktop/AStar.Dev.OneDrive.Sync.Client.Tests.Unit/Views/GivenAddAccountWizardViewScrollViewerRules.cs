using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenAddAccountWizardViewScrollViewerRules
{
    [AvaloniaFact]
    public void when_folder_list_scroll_viewer_is_inspected_then_it_is_not_hosted_inside_a_stack_panel()
    {
        var sut = new AddAccountWizardView();

        var stackPanelHostedScrollViewer = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(sv => sv.GetVisualParent() is StackPanel || sv.GetVisualParent()?.GetVisualParent() is StackPanel);

        stackPanelHostedScrollViewer.ShouldBeNull("folder-list ScrollViewer must not be hosted inside a StackPanel (unbounded height)");
    }

    [AvaloniaFact]
    public void when_folder_list_scroll_viewer_is_inspected_then_it_has_min_height_of_zero()
    {
        var sut = new AddAccountWizardView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.MinHeight == 0, "every vertically-scrollable ScrollViewer in AddAccountWizardView must declare MinHeight=0");
    }

    [AvaloniaFact]
    public void when_folder_list_scroll_viewer_is_inspected_then_it_sits_in_a_grid_star_row()
    {
        var sut = new AddAccountWizardView();

        var scrollViewers = sut.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
            .ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(sv => sv.GetVisualParent() is Grid, "folder-list ScrollViewer must be a direct child of a Grid, not a Border inside a StackPanel");
    }
}
