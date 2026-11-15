using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Ui;
using Avalonia.Headless.XUnit;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenTheMainViews
{
    [AvaloniaTheory]
    [InlineData("UnifiedAccounts")]
    [InlineData("GroupedByProvider")]
    [InlineData("SyncPairsFirst")]
    public async Task when_a_layout_is_shown_then_every_interactive_control_has_a_screen_reader_name(string layoutName)
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        viewModel.DisplaySettings.SelectLayout(LayoutNamed(layoutName));
        window.UpdateLayout();

        ScreenReaderNames.Unnamed(window).ShouldBeEmpty();
    }

    private static MainLayout LayoutNamed(string layoutName) => layoutName switch
    {
        "GroupedByProvider" => MainLayoutFactory.CreateGroupedByProvider(),
        "SyncPairsFirst" => MainLayoutFactory.CreateSyncPairsFirst(),
        _ => MainLayoutFactory.CreateUnifiedAccounts(),
    };
}
