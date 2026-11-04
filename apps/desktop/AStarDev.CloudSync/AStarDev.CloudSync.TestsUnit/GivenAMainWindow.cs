using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.ViewModels;
using AStarDev.Localization;
using Avalonia.Headless.XUnit;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenAMainWindow
{
    [AvaloniaFact]
    public void when_shown_then_the_window_is_visible()
    {
        var window = new MainWindow { DataContext = CreateViewModel() };

        window.Show();

        window.IsVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void when_shown_then_the_title_is_bound_from_the_view_model()
    {
        var window = new MainWindow { DataContext = CreateViewModel() };

        window.Show();

        window.Title.ShouldBe("Localised title");
    }

    [AvaloniaFact]
    public void when_shown_then_the_content_is_layout_a_bound_to_the_layout_a_view_model()
    {
        var viewModel = CreateViewModel();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();

        var layout = window.Content.ShouldBeOfType<LayoutAView>();
        layout.DataContext.ShouldBeSameAs(viewModel.LayoutA);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("App.Title").Returns("Localised title");
        var overview = new SyncOverviewViewModel(Substitute.For<ISyncOverviewSource>(), localization, TestData.CreateTime());

        return new MainWindowViewModel(localization, new LayoutAViewModel(overview, localization));
    }
}
