using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
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

public sealed partial class MainWindowViewModel(IApplicationInitializer initializer, ISyncScheduler scheduler, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings, FileClassificationRulesViewModel classificationRules, StatusBarViewModel statusBar, ILogger<MainWindowViewModel> logger) : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger = logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsClassificationsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    [NotifyPropertyChangedFor(nameof(ActiveView))]
    public partial NavSection ActiveSection { get; set; } = NavSection.Dashboard;

    public bool IsDashboardActive => ActiveSection == NavSection.Dashboard;
    public bool IsFilesActive => ActiveSection == NavSection.Files;
    public bool IsActivityActive => ActiveSection == NavSection.Activity;
    public bool IsAccountsActive => ActiveSection == NavSection.Accounts;
    public bool IsClassificationsActive => ActiveSection == NavSection.Classifications;
    public bool IsSettingsActive => ActiveSection == NavSection.Settings;

    [RelayCommand]
    private void Navigate(NavSection section) => ActiveSection = section;

    public object? ActiveView => ActiveSection switch
    {
        NavSection.Dashboard => DashboardViewInstance,
        NavSection.Files => FilesViewInstance,
        NavSection.Activity => ActivityViewInstance,
        NavSection.Accounts => AccountsViewInstance,
        NavSection.Classifications => FileClassificationsViewInstance,
        NavSection.Settings => SettingsViewInstance,
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
            .TapErrorAsync(e => OneDriveSyncClientMessages.MainWindowInitializeFatal(_logger, e.Message, e));

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
                    await files.ActivateAccountAsync(card.Id);
                    await activity.SetActiveAccountAsync(card.Id, card.Email);
                    return Unit.Default;
                })
                .TapErrorAsync(e => OneDriveSyncClientMessages.AccountSelectError(_logger, e.Message, e));
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.AccountSelectUnhandledError(_logger, ex.Message, ex);
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
                    ActiveSection = NavSection.Files;
                    await files.ActivateAccountAsync(account.Id.Id);
                    await activity.SetActiveAccountAsync(account.Id.Id, account.Profile.Email);
                    return Unit.Default;
                })
                .TapErrorAsync(e => OneDriveSyncClientMessages.AccountAddError(_logger, e.Message, e));
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.AccountAddUnhandledError(_logger, ex.Message, ex);
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
