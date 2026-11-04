using AStarDev.CloudSync.Controls;
using AStarDev.CloudSync.ViewModels;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheSharedControlSnapshots
{
    public static TheoryData<string, string> ControlsAndThemes()
    {
        var data = new TheoryData<string, string>();
        foreach (string control in new[] { "provider-badge", "status-pill", "storage-bar", "activity-row", "conflict-card", "stat-tile", "throughput-sparkline" })
        {
            data.Add(control, "dark");
            data.Add(control, "light");
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(ControlsAndThemes))]
    public async Task when_rendered_then_the_control_matches_its_baseline(string control, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
                Child = CreateControl(control, overview)
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-{control}-{theme}", 400, 160);
    }

    private static Control CreateControl(string control, SyncOverviewViewModel overview) => control switch
    {
        "provider-badge" => new ProviderBadge { DataContext = overview.Accounts[0].Provider },
        "status-pill" => new StatusPill { DataContext = overview.Accounts[0].Status },
        "storage-bar" => new StorageBar { DataContext = overview.Accounts[0].Storage },
        "activity-row" => new ActivityRow { DataContext = overview.Activity[0] },
        "conflict-card" => new ConflictCard { DataContext = overview.Conflicts[0] },
        "stat-tile" => new StatTile { DataContext = overview.Stats[0] },
        _ => new ThroughputSparkline { DataContext = overview.Throughput }
    };
}
