using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.ViewModels;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Layouts;

public class GivenALayoutBView
{
    [AvaloniaFact]
    public async Task when_shown_then_the_selected_account_and_its_folders_are_rendered()
    {
        var window = await CreateShownWindowAsync();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToList();

        texts.ShouldContain("OneDrive / Work");
        texts.ShouldContain("/home/user/CloudSync/OneDrive/Work/Documents");
        texts.ShouldNotContain("/home/user/CloudSync/OneDrive/Photos/Holiday photos");
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_four_tabs_are_present_with_overview_selected()
    {
        var window = await CreateShownWindowAsync();

        var tabs = window.GetVisualDescendants().OfType<TabControl>().ShouldHaveSingleItem();

        tabs.ItemCount.ShouldBe(4);
        tabs.SelectedIndex.ShouldBe(0);
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_scroll_viewer_has_a_bounded_viewport()
    {
        var window = await CreateShownWindowAsync();

        var scrollViewers = window.GetVisualDescendants().OfType<ScrollViewer>().ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(scrollViewer => scrollViewer.Viewport.Height <= window.Height);
    }

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutBView { DataContext = new LayoutBViewModel(overview, TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
