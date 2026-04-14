using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountCardViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountCardViewModel;
using AccountsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountsViewModel;
using ActivityViewModel = AStar.Dev.OneDrive.Sync.Client.Activity.ActivityViewModel;
using DashboardViewModel = AStar.Dev.OneDrive.Sync.Client.Dashboard.DashboardViewModel;
using SettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Settings.SettingsViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class MainWindowViewModel(IApplicationInitializer initializer, ISyncScheduler scheduler, IAccountRepository accountRepository, ISyncEventAggregator syncEventAggregator, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    [NotifyPropertyChangedFor(nameof(ActiveView))]
    public partial NavSection ActiveSection { get; set; } = NavSection.Dashboard;

    public bool IsDashboardActive => ActiveSection == NavSection.Dashboard;
    public bool IsFilesActive => ActiveSection == NavSection.Files;
    public bool IsActivityActive => ActiveSection == NavSection.Activity;
    public bool IsAccountsActive => ActiveSection == NavSection.Accounts;
    public bool IsSettingsActive => ActiveSection == NavSection.Settings;

    [RelayCommand]
    private void Navigate(NavSection section) => ActiveSection = section;

    public object? ActiveView
    {
        get
        {
            object? result = ActiveSection switch
            {
                NavSection.Dashboard => DashboardViewInstance,
                NavSection.Files     => FilesViewInstance,
                NavSection.Activity  => ActivityViewInstance,
                NavSection.Accounts  => AccountsViewInstance,
                NavSection.Settings  => SettingsViewInstance,
                _                    => null
            };

            return result;
        }
    }

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

    public AccountsViewModel Accounts => accounts;

    public FilesViewModel Files => files;

    public ActivityViewModel Activity => activity;

    public DashboardViewModel Dashboard => dashboard;

    public SettingsViewModel Settings => settings;

    public StatusBarViewModel StatusBar { get; } = new();

    public async Task InitialiseAsync()
    {
        try
        {
            syncEventAggregator.SyncProgressChanged += OnSyncProgressChanged;
            syncEventAggregator.SyncCompleted += OnSyncCompleted;
            syncEventAggregator.ConflictDetected += OnConflictDetected;

            accounts.AccountSelected += OnAccountSelectedAsync;
            accounts.AccountAdded += OnAccountAddedAsync;
            accounts.AccountRemoved += OnAccountRemoved;
            accounts.ActiveAccountStateChanged += (_, _) => SyncStatusBarToActiveAccount();
            accounts.PropertyChanged += (_, e) =>
            {
                if(e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
                    SyncStatusBarToActiveAccount();
            };

            await initializer.InitializeAsync();

            SyncStatusBarToActiveAccount();
        }
        catch(Exception ex)
        {
            Serilog.Log.Fatal(ex, "[MainWindowViewModel.InitialiseAsync] FATAL ERROR: {Error}", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        var active = accounts.ActiveAccount;
        if(active is null)
            return;

        var entity = await accountRepository.GetByIdAsync(active.Id, CancellationToken.None);
        if(entity is null)
            return;

        var account = new OneDriveAccount
        {
            Id                = entity.Id,
            DisplayName       = entity.DisplayName,
            Email             = entity.Email,
            LocalSyncPath     = entity.LocalSyncPath,
            ConflictPolicy    = entity.ConflictPolicy,
            SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)],
            LastSyncedAt      = entity.LastSyncedAt
        };

        await scheduler.TriggerAccountAsync(account);
    }

    [RelayCommand]
    private void AddAccount()
    {
        ActiveSection = NavSection.Accounts;
        accounts.AddAccount();
    }

    private async void OnAccountSelectedAsync(object? sender, AccountCardViewModel card)
        => await Try.RunAsync(async () =>
            {
                ActiveSection = NavSection.Files;
                await files.ActivateAccountAsync(card.Id);
                await activity.SetActiveAccountAsync(card.Id, card.Email);
                SyncStatusBarToActiveAccount();
                return Unit.Default;
            })
            .TapErrorAsync(e=> Serilog.Log.Error(e, "[MainWindowViewModel.OnAccountSelectedAsync] Error: {Error}", e));

    private async void OnAccountAddedAsync(object? sender, OneDriveAccount account)
        => await Try.RunAsync(async () =>
            {
                files.AddAccount(account);
                dashboard.AddAccount(account);
                settings.AddAccount(account);
                ActiveSection = NavSection.Files;
                await files.ActivateAccountAsync(account.Id);
                await activity.SetActiveAccountAsync(account.Id, account.Email);
                return Unit.Default;
            })
            .TapErrorAsync(e=> Serilog.Log.Error(e, "[MainWindowViewModel.OnAccountAddedAsync] Error: {Error}", e));

    private void OnAccountRemoved(object? sender, string accountId)
    {
        files.RemoveAccount(accountId);
        dashboard.RemoveAccount(accountId);
        settings.RemoveAccount(accountId);
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        if(accounts.Accounts.Any(a => a.Id == e.AccountId && a.Id == accounts.ActiveAccount?.Id))
            SyncStatusBarToActiveAccount();
    }

    private void OnSyncCompleted(object? sender, string accountId)
    {
        if(accounts.ActiveAccount?.Id == accountId)
            SyncStatusBarToActiveAccount();
    }

    private void OnConflictDetected(object? sender, SyncConflict conflict)
    {
        if(accounts.ActiveAccount?.Id == conflict.AccountId)
            SyncStatusBarToActiveAccount();
    }

    private void SyncStatusBarToActiveAccount()
    {
        var active = accounts.ActiveAccount;
        if(active is null)
        {
            StatusBar.HasAccount = false;
            StatusBar.AccountEmail = string.Empty;
            StatusBar.AccountDisplayName = string.Empty;
            return;
        }

        StatusBar.HasAccount = true;
        StatusBar.AccountEmail = active.Email;
        StatusBar.AccountDisplayName = active.DisplayName;
        StatusBar.SyncState = active.SyncState;
        StatusBar.ConflictCount = active.ConflictCount;
        StatusBar.LastSyncText = active.LastSyncText;
        StatusBar.IsSyncing = active.SyncState == SyncState.Syncing;
    }
}
