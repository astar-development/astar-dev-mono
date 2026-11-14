using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheFailedSyncSnapshots
{
    [AvaloniaTheory]
    [InlineData("a", "dark")]
    [InlineData("a", "light")]
    [InlineData("b", "dark")]
    [InlineData("b", "light")]
    [InlineData("c", "dark")]
    [InlineData("c", "light")]
    public async Task when_the_sync_failed_then_the_layout_matches_the_baseline(string layout, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var window = await CreateWindowAsync(layout, TestData.CreateAccount(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateRootUnavailable()), Option.Some(TestData.Now.AddHours(-3))));

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"layout-{layout}-failed-{theme}");
    }

    private static async Task<Window> CreateWindowAsync(string layout, CloudAccount account)
    {
        var overview = new SyncOverviewViewModel(new FixedOverviewSource(SyncOverviewFactory.Create([account], [], [], [], [], 0, Option.None<DateTimeOffset>())), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);
        var actions = new RecordingSyncActions();
        var localization = TestData.CreateLocalization();
        Control view = layout switch
        {
            "a" => new LayoutAView { DataContext = new LayoutAViewModel(overview, actions, localization) },
            "b" => new LayoutBView { DataContext = new LayoutBViewModel(overview, TestData.CreateLocalPathFormatter(), actions, localization) },
            _ => new LayoutCView { DataContext = new LayoutCViewModel(overview, actions, localization) }
        };

        return new Window { Content = view };
    }

    private sealed class FixedOverviewSource(SyncOverview overview) : ISyncOverviewSource
    {
        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(overview));
    }
}
