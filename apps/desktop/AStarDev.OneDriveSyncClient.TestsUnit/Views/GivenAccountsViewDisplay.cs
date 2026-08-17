using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Onboarding;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Onboarding;
using AStarDev.OneDriveSyncClient.Search;
using AStarDev.OneDriveSyncClient.Settings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Views;

[Collection("AvaloniaHeadless")]
public sealed class GivenAccountsViewDisplay
{
    private static ILocalizationService CreateLocalization()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        return localization;
    }

    private static AccountsViewModel CreateAccountsViewModel()
    {
        return new AccountsViewModel(Substitute.For<IAuthService>(), Substitute.For<IGraphService>(), Substitute.For<IAccountRepository>(), Substitute.For<IAccountOnboardingService>(), Substitute.For<IQuotaRefreshService>(), Substitute.For<ISyncEventAggregator>(), Substitute.For<IAddAccountWizardViewModelFactory>(), Substitute.For<IAccountCardViewModelFactory>(), Substitute.For<ILogger<AccountsViewModel>>());
    }

    private static MainWindowViewModel CreateMainWindowViewModel(AccountsViewModel accounts)
    {
        var localization = CreateLocalization();
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.Current.Returns(new AppSettings());

        var files = new FilesViewModel(Substitute.For<IAccountFilesViewModelFactory>(), localization);
        var dashboard = new DashboardViewModel(localization, Substitute.For<ISyncEventAggregator>(), Substitute.For<IDashboardAccountViewModelFactory>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<IUiTimer>());
        var activity = new ActivityViewModel(Substitute.For<ISyncRepository>(), Substitute.For<ISyncEventAggregator>(), Substitute.For<IConflictItemViewModelFactory>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<IUiDispatcher>(), localization);
        var settings = new SettingsViewModel(settingsService, Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), localization, Substitute.For<IFolderPickerService>());
        var classificationRules = new FileClassificationRulesViewModel(Substitute.For<IFileClassificationRepository>(), Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), Substitute.For<ICategoryEditDialogService>(), localization, Substitute.For<IFileSystem>());
        var statusBar = new StatusBarViewModel(accounts, localization);

        return new MainWindowViewModel(Substitute.For<IApplicationInitializer>(), Substitute.For<ISyncScheduler>(), accounts, files, dashboard, activity, settings, classificationRules, new SyncedFileSearchViewModel(Substitute.For<ISyncedItemRepository>(), Substitute.For<IFileOpenerService>(), Substitute.For<IFileTypeClassifier>(), Substitute.For<IUiDispatcher>(), localization), statusBar, localization, Substitute.For<ILogger<MainWindowViewModel>>());
    }

    private static AccountsView CreateViewWithViewModel(MainWindowViewModel viewModel)
    {
        var view = new AccountsView { DataContext = viewModel };
        view.Measure(new(1000, 800));
        view.Arrange(new(0, 0, 1000, 800));

        return view;
    }

    [AvaloniaFact]
    public void when_no_accounts_exist_then_empty_state_panel_is_visible()
    {
        var viewModel = CreateMainWindowViewModel(CreateAccountsViewModel());

        var sut = CreateViewWithViewModel(viewModel);

        var emptyState = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => sp.IsVisible && sp.Children.OfType<TextBlock>().Any(tb => tb.Text == viewModel.NoAccountsAddedYetText));
        emptyState.ShouldNotBeNull("Empty-state panel should be visible when no accounts are connected");
    }

    [AvaloniaFact]
    public void when_no_accounts_exist_then_account_list_scroll_viewer_is_hidden()
    {
        var viewModel = CreateMainWindowViewModel(CreateAccountsViewModel());

        var sut = CreateViewWithViewModel(viewModel);

        var scrollViewer = sut.GetLogicalDescendants().OfType<ScrollViewer>().FirstOrDefault(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto && sv.MinHeight == 0);
        scrollViewer.ShouldNotBeNull();
        scrollViewer.IsVisible.ShouldBeFalse("Account list ScrollViewer should be hidden when HasAccounts is false");
    }

    [AvaloniaFact]
    public void when_no_wizard_is_active_then_wizard_overlay_is_hidden()
    {
        var viewModel = CreateMainWindowViewModel(CreateAccountsViewModel());

        var sut = CreateViewWithViewModel(viewModel);

        var wizardBorder = sut.GetLogicalDescendants().OfType<Border>().First(b => b.Width == 480);
        var overlayGrid = (Grid)wizardBorder.GetLogicalParent()!;
        overlayGrid.IsVisible.ShouldBeFalse("Wizard overlay should be hidden when no wizard is in progress");
    }

    [AvaloniaFact]
    public void when_wizard_is_started_after_render_then_wizard_overlay_becomes_visible()
    {
        var accounts = CreateAccountsViewModel();
        var viewModel = CreateMainWindowViewModel(accounts);
        var sut = CreateViewWithViewModel(viewModel);

        accounts.Wizard = new AddAccountWizardViewModel(Substitute.For<IAuthService>(), Substitute.For<IGraphService>(), CreateLocalization());

        var wizardBorder = sut.GetLogicalDescendants().OfType<Border>().First(b => b.Width == 480);
        var overlayGrid = (Grid)wizardBorder.GetLogicalParent()!;
        overlayGrid.IsVisible.ShouldBeTrue("Wizard overlay should become visible when the wizard view model is set");
    }

    [AvaloniaFact]
    public void when_account_list_is_inspected_then_items_control_is_bound_to_accounts_collection()
    {
        var viewModel = CreateMainWindowViewModel(CreateAccountsViewModel());

        var sut = CreateViewWithViewModel(viewModel);

        var itemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.Accounts.Accounts));
        itemsControl.ShouldNotBeNull("Account card ItemsControl should be bound to Accounts.Accounts");
    }

    [AvaloniaFact]
    public void when_no_accounts_exist_then_add_account_button_is_available_in_empty_state()
    {
        var viewModel = CreateMainWindowViewModel(CreateAccountsViewModel());

        var sut = CreateViewWithViewModel(viewModel);

        var addAccountButtons = sut.GetLogicalDescendants().OfType<Button>().Where(b => b.Command == viewModel.AddAccountCommand && b.IsVisible).ToList();
        addAccountButtons.ShouldNotBeEmpty("Add-account button should be visible in the empty state");
    }

    [AvaloniaFact]
    public void when_empty_state_is_shown_then_personal_hint_text_is_present()
    {
        var viewModel = CreateMainWindowViewModel(CreateAccountsViewModel());

        var sut = CreateViewWithViewModel(viewModel);

        var hintBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == viewModel.NoAccountsPersonalHintText);
        hintBlock.ShouldNotBeNull("Personal-account hint TextBlock should be present in the empty state");
    }
}
