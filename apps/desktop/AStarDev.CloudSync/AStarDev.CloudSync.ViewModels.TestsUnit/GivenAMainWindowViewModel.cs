using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.DisplaySettings;

namespace AStarDev.CloudSync.ViewModels.TestsUnit;

public class GivenAMainWindowViewModel
{
    [Fact]
    public async Task when_created_then_the_title_comes_from_the_app_title_localisation_key()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());

        viewModel.Title.ShouldBe("AStar Dev CloudSync");
    }

    [Fact]
    public async Task when_nothing_has_been_chosen_then_layout_a_is_the_current_layout()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());

        viewModel.CurrentLayout.ShouldBeSameAs(viewModel.LayoutA);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("C")]
    public async Task when_another_layout_is_chosen_then_it_becomes_current_and_the_change_is_raised(string key)
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.FromKey(key));

        viewModel.CurrentLayout.ShouldBeSameAs(key == "B" ? viewModel.LayoutB : viewModel.LayoutC);
        changed.ShouldContain(nameof(MainWindowViewModel.CurrentLayout));
    }

    [Fact]
    public async Task when_the_layout_is_switched_away_and_back_then_the_selected_account_and_filter_are_kept()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var photos = viewModel.LayoutB.Overview.Accounts.Single(account => account.Name == "Photos");
        viewModel.LayoutA.ProviderChips[1].SelectCommand.Execute(null);
        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateGroupedByProvider());
        viewModel.LayoutB.SelectedItem = photos;

        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateUnifiedAccounts());
        viewModel.DisplaySettings.SelectLayout(MainLayoutFactory.CreateGroupedByProvider());

        viewModel.LayoutB.SelectedItem.ShouldBeSameAs(photos);
        viewModel.LayoutA.ProviderChips[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_layout_was_saved_then_the_window_starts_on_it()
    {
        var store = new InMemoryDisplayPreferencesStore(DisplayPreferencesFactory.Create(MainLayoutFactory.CreateSyncPairsFirst(), AppThemeFactory.CreateDark()));
        var settings = new DisplaySettingsViewModel(store, new RecordingThemeApplier(), TestData.CreatePlaintextCacheConsentSettings(), TestData.CreateLocalization());

        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), settings);

        viewModel.CurrentLayout.ShouldBeSameAs(viewModel.LayoutC);
    }

    [Fact]
    public async Task when_the_account_dialog_opens_then_a_dialog_is_open()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = TestData.CreateMainWindowViewModel(overview, TestData.CreateDisplaySettings());
        var opened = new List<string?>();
        window.PropertyChanged += (_, args) => opened.Add(args.PropertyName);

        _ = window.AccountDialog.RemoveAsync(overview.Accounts[0], TestContext.Current.CancellationToken);

        (window.IsDialogOpen, opened.Contains(nameof(window.IsDialogOpen))).ShouldBe((true, true));
        window.AccountDialog.CloseCommand.Execute(null);
        window.IsDialogOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_folder_selection_dialog_opens_and_closes_then_the_dialog_open_flag_follows_and_is_raised()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _ = viewModel.FolderSelection.ShowAsync(Option.None<AStarDev.CloudSync.Accounts.AccountId>(), TestContext.Current.CancellationToken);
        var whileOpen = viewModel.IsDialogOpen;
        viewModel.FolderSelection.CancelCommand.Execute(null);

        (whileOpen, viewModel.IsDialogOpen, raised.Count(name => name == nameof(MainWindowViewModel.IsDialogOpen))).ShouldBe((true, false, 2));
    }

    [Fact]
    public async Task when_the_sync_restart_question_opens_and_closes_then_the_dialog_flag_follows_and_is_raised_each_time()
    {
        var viewModel = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _ = viewModel.SyncRestart.AskAsync(AStarDev.CloudSync.Accounts.AccountIdFactory.Create(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var whileOpen = viewModel.IsDialogOpen;
        viewModel.SyncRestart.KeepCommand.Execute(null);

        (whileOpen, viewModel.IsDialogOpen, raised.Count(name => name == nameof(MainWindowViewModel.IsDialogOpen))).ShouldBe((true, false, 2));
    }
}
