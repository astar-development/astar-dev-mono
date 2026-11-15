using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.Tray;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

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

    [AvaloniaFact]
    public async Task when_the_display_settings_are_shown_then_every_interactive_control_has_a_screen_reader_name()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var window = new Window { Content = new DisplaySettingsView { DataContext = viewModel.DisplaySettings } };

        window.Show();

        ScreenReaderNames.Unnamed(window).ShouldBeEmpty();
    }

    [AvaloniaFact]
    public async Task when_the_quit_question_is_open_then_every_interactive_control_has_a_screen_reader_name()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var window = new Window { Content = new ConfirmQuitView { DataContext = viewModel.ConfirmQuit } };
        window.Show();

        _ = viewModel.ConfirmQuit.AskAsync(TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();

        ScreenReaderNames.Unnamed(window).ShouldBeEmpty();
    }

    private static MainLayout LayoutNamed(string layoutName) => layoutName switch
    {
        "GroupedByProvider" => MainLayoutFactory.CreateGroupedByProvider(),
        "SyncPairsFirst" => MainLayoutFactory.CreateSyncPairsFirst(),
        _ => MainLayoutFactory.CreateUnifiedAccounts(),
    };
}
