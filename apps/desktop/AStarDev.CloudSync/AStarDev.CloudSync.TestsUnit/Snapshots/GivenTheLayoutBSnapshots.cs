using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Layouts;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheLayoutBSnapshots
{
    [AvaloniaFact]
    public async Task when_rendered_in_the_dark_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(await CreateWindowAsync(), ThemeVariant.Dark, "layout-b-dark");
    }

    [AvaloniaFact]
    public async Task when_rendered_in_the_light_theme_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(await CreateWindowAsync(), ThemeVariant.Light, "layout-b-light");
    }

    private static async Task<Window> CreateWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();

        return new Window { Content = new LayoutBView { DataContext = new LayoutBViewModel(overview, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateLocalization()) } };
    }
}
