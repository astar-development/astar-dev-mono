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

    private static MainWindowViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("App.Title").Returns("Localised title");

        return new MainWindowViewModel(localization);
    }
}
