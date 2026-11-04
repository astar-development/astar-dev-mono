using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.ViewModels;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Layouts;

public class GivenALayoutAView
{
    [AvaloniaFact]
    public async Task when_shown_at_the_default_window_size_then_the_account_cards_scroll()
    {
        var window = await CreateShownWindowAsync();

        var cards = window.GetVisualDescendants().OfType<ScrollViewer>().Single(scrollViewer => scrollViewer.Name == "AccountCards");

        cards.Extent.Height.ShouldBeGreaterThan(cards.Viewport.Height);
        cards.Viewport.Height.ShouldBeGreaterThan(0);
    }

    [AvaloniaFact]
    public async Task when_shown_then_a_card_is_rendered_for_every_visible_account()
    {
        var window = await CreateShownWindowAsync();

        window.GetVisualDescendants().OfType<Border>().Count(border => border.Classes.Contains("account-card")).ShouldBe(4);
    }

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
