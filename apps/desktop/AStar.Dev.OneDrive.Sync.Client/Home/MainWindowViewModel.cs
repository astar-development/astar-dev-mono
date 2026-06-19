using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Search;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AccountCardViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountCardViewModel;
using AccountsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountsViewModel;
using ActivityViewModel = AStar.Dev.OneDrive.Sync.Client.Activity.ActivityViewModel;
using DashboardViewModel = AStar.Dev.OneDrive.Sync.Client.Dashboard.DashboardViewModel;
using SettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Settings.SettingsViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class MainWindowViewModel(IApplicationInitializer initializer, ISyncScheduler scheduler, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings, FileClassificationRulesViewModel classificationRules, SyncedFileSearchViewModel search, StatusBarViewModel statusBar, ILocalizationService localizationService, ILogger<MainWindowViewModel> logger) : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> logger = logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsClassificationsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(ActiveView))]
    public partial NavSection ActiveSection { get; set; } = NavSection.Dashboard;

    public bool IsDashboardActive => ActiveSection == NavSection.Dashboard;
    public bool IsFilesActive => ActiveSection == NavSection.Files;
    public bool IsActivityActive => ActiveSection == NavSection.Activity;
    public bool IsAccountsActive => ActiveSection == NavSection.Accounts;
    public bool IsClassificationsActive => ActiveSection == NavSection.Classifications;
    public bool IsSettingsActive => ActiveSection == NavSection.Settings;
    public bool IsSearchActive => ActiveSection == NavSection.Search;

    /// <summary>Localised "Accounts" panel heading.</summary>
    public string AccountsPanelHeading => localizationService.GetLocal("MainWindow.Accounts");

    /// <summary>Localised empty-state heading when no accounts are added.</summary>
    public string NoAccountsYetText => localizationService.GetLocal("MainWindow.NoAccountsYet");

    /// <summary>Localised empty-state hint when no accounts are added.</summary>
    public string NoAccountsYetHintText => localizationService.GetLocal("MainWindow.NoAccountsYetHint");

    /// <summary>Localised "Add account" button label.</summary>
    public string AddAccountText => localizationService.GetLocal("MainWindow.AddAccount");

    /// <summary>Localised status-bar text when no account is selected.</summary>
    public string NoAccountSelectedText => localizationService.GetLocal("MainWindow.NoAccountSelected");

    /// <summary>Localised "No accounts added yet" heading in the accounts view empty state.</summary>
    public string NoAccountsAddedYetText => localizationService.GetLocal("Account.NoAccounts");

    /// <summary>Localised hint in the accounts view empty state.</summary>
    public string NoAccountsPersonalHintText => localizationService.GetLocal("Account.NoAccountsHint2");

    /// <summary>Localised "Add account" label for the accounts view button.</summary>
    public string AddAccountButtonText => localizationService.GetLocal("Account.AddAccount");

    /// <summary>Localised "Connected accounts" heading in the accounts list.</summary>
    public string ConnectedAccountsText => localizationService.GetLocal("Account.ConnectedAccounts");

    /// <summary>Localised "Sign in again" label for re-auth button.</summary>
    public string SignInAgainText => localizationService.GetLocal("Account.SignInAgain");

    /// <summary>Localised "Add another account" label.</summary>
    public string AddAnotherAccountText => localizationService.GetLocal("Account.AddAnotherAccount");

    /// <summary>Localised "Remove account" tooltip for the remove button.</summary>
    public string RemoveAccountText => localizationService.GetLocal("Account.RemoveAccount");

    /// <summary>Localised tooltip for the Dashboard nav rail button.</summary>
    public string NavTooltipDashboardText => localizationService.GetLocal("Nav.Tooltip.Dashboard");

    /// <summary>Localised tooltip for the Files nav rail button.</summary>
    public string NavTooltipFilesText => localizationService.GetLocal("Nav.Tooltip.Files");

    /// <summary>Localised tooltip for the Activity nav rail button.</summary>
    public string NavTooltipActivityText => localizationService.GetLocal("Nav.Tooltip.Activity");

    /// <summary>Localised tooltip for the Accounts nav rail button.</summary>
    public string NavTooltipAccountsText => localizationService.GetLocal("Nav.Tooltip.Accounts");

    /// <summary>Localised tooltip for the Classifications nav rail button.</summary>
    public string NavTooltipClassificationsText => localizationService.GetLocal("Nav.Tooltip.Classifications");

    /// <summary>Localised tooltip for the Settings nav rail button.</summary>
    public string NavTooltipSettingsText => localizationService.GetLocal("Nav.Tooltip.Settings");

    /// <summary>Localised tooltip for the Search nav rail button.</summary>
    public string NavTooltipSearchText => localizationService.GetLocal("Nav.Tooltip.Search");

    [RelayCommand]
    private void Navigate(NavSection section) => ActiveSection = section;

    partial void OnActiveSectionChanged(NavSection value)
    {
        if (value == NavSection.Search)
            _ = search.OnViewActivatedAsync(CancellationToken.None);
    }

    public object? ActiveView => ActiveSection switch
    {
        NavSection.Dashboard => DashboardViewInstance,
        NavSection.Files => FilesViewInstance,
        NavSection.Activity => ActivityViewInstance,
        NavSection.Accounts => AccountsViewInstance,
        NavSection.Classifications => FileClassificationsViewInstance,
        NavSection.Settings => SettingsViewInstance,
        NavSection.Search => SearchViewInstance,
        _ => null
    };

    private DashboardView DashboardViewInstance
    {
        get
        {
            field ??= new DashboardView { DataContext = dashboard };

            return field;
        }
    }

    private FilesView FilesViewInstance
    {
        get
        {
            field ??= new FilesView { DataContext = files };

            return field;
        }
    }

    private ActivityView ActivityViewInstance
    {
        get
        {
            field ??= new ActivityView { DataContext = activity };

            return field;
        }
    }

    private AccountsView AccountsViewInstance
    {
        get
        {
            field ??= new AccountsView { DataContext = this };

            return field;
        }
    }

    private SettingsView SettingsViewInstance
    {
        get
        {
            field ??= new SettingsView { DataContext = settings };

            return field;
        }
    }

    private FileClassificationsView FileClassificationsViewInstance
    {
        get
        {
            field ??= new FileClassificationsView { DataContext = classificationRules };

            return field;
        }
    }

    private SyncedFileSearchView SearchViewInstance
    {
        get
        {
            field ??= new SyncedFileSearchView { DataContext = search };

            return field;
        }
    }

    public AccountsViewModel Accounts => accounts;

    public FilesViewModel Files => files;

    public ActivityViewModel Activity => activity;

    public DashboardViewModel Dashboard => dashboard;

    public SettingsViewModel Settings => settings;

    public StatusBarViewModel StatusBar => statusBar;

    public async Task InitialiseAsync()
        => await Try.RunAsync(async () =>
            {
                accounts.AccountSelected += OnAccountSelectedAsync;
                accounts.AccountAdded += OnAccountAddedAsync;
                accounts.AccountRemoved += OnAccountRemoved;

                files.FolderCountChanged += OnFolderCountChanged;
                await initializer.InitializeAsync();
                return Unit.Default;
            })
            .TapErrorAsync(e => OneDriveSyncClientMessages.MainWindowInitializeFatal(logger, e.Message, e));

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        var active = accounts.ActiveAccount;
        if (active is null)
            return;

        await scheduler.TriggerAccountAsync(active.Id).ConfigureAwait(false);
    }

    [RelayCommand]
    private void AddAccount()
    {
        ActiveSection = NavSection.Accounts;
        accounts.AddAccount();
    }

    private async void OnAccountSelectedAsync(object? sender, AccountCardViewModel card)
    {
        try
        {
            await Try.RunAsync(async () =>
                {
                    ActiveSection = NavSection.Files;
                    search.SetActiveAccount(new AccountId(card.Id));
                    await files.ActivateAccountAsync(card.Id);
                    await activity.SetActiveAccountAsync(card.Id, card.Email);
                    return Unit.Default;
                })
                .TapErrorAsync(e => OneDriveSyncClientMessages.AccountSelectError(logger, e.Message, e));
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.AccountSelectUnhandledError(logger, ex.Message, ex);
        }
    }

    private async void OnAccountAddedAsync(object? sender, OneDriveAccount account)
    {
        try
        {
            await Try.RunAsync(async () =>
                {
                    files.AddAccount(account);
                    dashboard.AddAccount(account);
                    settings.AddAccount(account);
                    search.SetActiveAccount(account.Id);
                    ActiveSection = NavSection.Files;
                    await files.ActivateAccountAsync(account.Id.Id);
                    await activity.SetActiveAccountAsync(account.Id.Id, account.Profile.Email);
                    return Unit.Default;
                })
                .TapErrorAsync(e => OneDriveSyncClientMessages.AccountAddError(logger, e.Message, e));
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.AccountAddUnhandledError(logger, ex.Message, ex);
        }
    }

    private void OnAccountRemoved(object? sender, string accountId)
    {
        files.RemoveAccount(accountId);
        dashboard.RemoveAccount(accountId);
        settings.RemoveAccount(accountId);
    }

    private void OnFolderCountChanged(object? sender, (string AccountId, int FolderCount) args)
        => dashboard.UpdateFolderCount(args.AccountId, args.FolderCount);
}
