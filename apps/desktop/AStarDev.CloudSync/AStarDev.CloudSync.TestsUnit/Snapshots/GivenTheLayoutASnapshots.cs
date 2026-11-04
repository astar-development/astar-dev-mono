using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.ViewModels;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheLayoutASnapshots
{
    [AvaloniaFact]
    public async Task when_rendered_in_the_dark_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(await CreateWindowAsync(), ThemeVariant.Dark, "layout-a-dark");
    }

    [AvaloniaFact]
    public async Task when_rendered_in_the_light_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(await CreateWindowAsync(), ThemeVariant.Light, "layout-a-light");
    }

    private static async Task<Window> CreateWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();

        return new Window { Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, TestData.CreateLocalization()) } };
    }
}
