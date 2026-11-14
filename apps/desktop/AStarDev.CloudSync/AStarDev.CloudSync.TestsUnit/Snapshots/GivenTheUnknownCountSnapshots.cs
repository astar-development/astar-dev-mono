using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.Overview;
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

public class GivenTheUnknownCountSnapshots
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_an_account_has_unknown_counts_then_layout_a_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var overview = await CreateOverviewAsync();

        Snapshot.ShouldMatchBaseline(new Window { Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization()) } }, ThemeOf(theme), $"layout-a-unknown-counts-{theme}");
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_an_account_has_unknown_counts_then_layout_b_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var overview = await CreateOverviewAsync();

        Snapshot.ShouldMatchBaseline(new Window { Content = new LayoutBView { DataContext = new LayoutBViewModel(overview, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateLocalization()) } }, ThemeOf(theme), $"layout-b-unknown-counts-{theme}");
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_an_account_has_unknown_counts_then_layout_c_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var overview = await CreateOverviewAsync();

        Snapshot.ShouldMatchBaseline(new Window { Content = new LayoutCView { DataContext = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization()) } }, ThemeOf(theme), $"layout-c-unknown-counts-{theme}");
    }

    private static ThemeVariant ThemeOf(string theme) => theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static async Task<SyncOverviewViewModel> CreateOverviewAsync()
    {
        var overview = new SyncOverviewViewModel(new FixedOverviewSource(SyncOverviewFactory.Create([TestData.CreateAccountWithUnknownCounts()], [], [], [], [], 0, Option.None<DateTimeOffset>())), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);

        return overview;
    }

    private sealed class FixedOverviewSource(SyncOverview overview) : ISyncOverviewSource
    {
        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(overview));
    }
}
