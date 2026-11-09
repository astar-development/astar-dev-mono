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

public class GivenTheSignInRequiredCardSnapshot
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_an_account_needs_sign_in_then_layout_a_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var overview = new SyncOverviewViewModel(new FixedOverviewSource(SyncOverviewFactory.Create([TestData.CreateAccountNeedingSignIn()], [], [], [], [], 0, Option.None<DateTimeOffset>())), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);
        var window = new Window { Content = new LayoutAView { DataContext = new LayoutAViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization()) } };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"layout-a-sign-in-required-{theme}");
    }

    private sealed class FixedOverviewSource(SyncOverview overview) : ISyncOverviewSource
    {
        public Task<Result<SyncOverview, AStarDev.FunctionalParadigm.ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, AStarDev.FunctionalParadigm.ErrorResponse>(overview));
    }
}
