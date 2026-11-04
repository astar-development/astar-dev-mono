using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheMainWindowSnapshot
{
    [AvaloniaFact]
    public async Task when_rendered_in_the_dark_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(await CreateWindowAsync(), ThemeVariant.Dark, "main-window-dark");
    }

    [AvaloniaFact]
    public async Task when_rendered_in_the_light_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(await CreateWindowAsync(), ThemeVariant.Light, "main-window-light");
    }

    private static async Task<MainWindow> CreateWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();

        return new MainWindow { DataContext = TestData.CreateMainWindowViewModel(overview, TestData.CreateDisplaySettings()) };
    }
}
