using AStarDev.CloudSync.ViewModels;
using AStarDev.Localization;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheMainWindowSnapshot
{
    [AvaloniaFact]
    public void when_rendered_in_the_dark_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(CreateWindow(), ThemeVariant.Dark, "main-window-dark");
    }

    [AvaloniaFact]
    public void when_rendered_in_the_light_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(CreateWindow(), ThemeVariant.Light, "main-window-light");
    }

    private static MainWindow CreateWindow()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("App.Title").Returns("AStar Dev CloudSync");

        return new MainWindow { DataContext = new MainWindowViewModel(localization) };
    }
}
