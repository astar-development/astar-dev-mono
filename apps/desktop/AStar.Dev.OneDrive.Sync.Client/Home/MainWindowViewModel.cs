using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.Views;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class MainWindowViewModel(
    IAuthService authService,
    IGraphService graphService,
    IStartupService startupService,
    ISyncService syncService,
    SyncScheduler scheduler,
    ISyncRepository syncRepository,
    ISettingsService settingsService,
    IAccountRepository accountRepository) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    [NotifyPropertyChangedFor(nameof(ActiveView))]
    private NavSection _activeSection = NavSection.Dashboard;

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
            field ??= new DashboardView { DataContext = Dashboard };

            return field;
        }
    }

    private FilesView FilesViewInstance
    {
        get
        {
            field ??= new FilesView { DataContext = Files };

            return field;
        }
    }

    private ActivityView ActivityViewInstance
    {
        get
        {
            field ??= new ActivityView { DataContext = Activity };

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
            field ??= new SettingsView { DataContext = Settings };

            return field;
        }
    }

    public AccountsViewModel Accounts { get; } = new(authService, graphService, App.AccountRepository);

    public FilesViewModel Files { get; } = new(authService, graphService, App.AccountRepository);

    public ActivityViewModel Activity { get; } = new(syncService, syncRepository);

    public DashboardViewModel Dashboard { get; } = new(scheduler);

    public SettingsViewModel Settings { get; } = new(settingsService, App.Theme, scheduler, accountRepository);

    public StatusBarViewModel StatusBar { get; } = new();

    public async Task InitialiseAsync()
    {
        try
        {
            syncService.SyncProgressChanged += OnSyncProgressChanged;
            syncService.JobCompleted += OnJobCompleted;
            syncService.ConflictDetected += OnConflictDetected;

            Accounts.AccountSelected += OnAccountSelected;
            Accounts.AccountAdded += OnAccountAdded;
            Accounts.AccountRemoved += OnAccountRemoved;
            Accounts.PropertyChanged += (_, e) =>
            {
                if(e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
                    SyncStatusBarToActiveAccount();
            };

            var restored = await startupService.RestoreAccountsAsync();

            Accounts.RestoreAccounts(restored);

            foreach(var account in restored)
            {
                Files.AddAccount(account);
                Dashboard.AddAccount(account);
            }

            Settings.LoadAccounts(restored);

            var active = restored.FirstOrDefault(a => a.IsActive);
            if(active is not null)
            {
                await Files.ActivateAccountAsync(active.Id);

                await Activity.SetActiveAccountAsync(active.Id, active.Email);
            }

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
        var active = Accounts.ActiveAccount;
        if(active is null)
            return;

        var entity = await App.AccountRepository.GetByIdAsync(active.Id);
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
        Accounts.AddAccount();
    }

    private async void OnAccountSelected(object? sender, AccountCardViewModel card)
    {
        ActiveSection = NavSection.Files;
        await Files.ActivateAccountAsync(card.Id);
        await Activity.SetActiveAccountAsync(card.Id, card.Email);
        SyncStatusBarToActiveAccount();
    }

    private async void OnAccountAdded(object? sender, OneDriveAccount account)
    {
        Files.AddAccount(account);
        Dashboard.AddAccount(account);
        Settings.AddAccount(account);
        ActiveSection = NavSection.Files;
        await Files.ActivateAccountAsync(account.Id);
        await Activity.SetActiveAccountAsync(account.Id, account.Email);
    }

    private void OnAccountRemoved(object? sender, string accountId)
    {
        Files.RemoveAccount(accountId);
        Dashboard.RemoveAccount(accountId);
        Settings.RemoveAccount(accountId);
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
        => Dispatcher.UIThread.Post(() =>
            {
                var card = Accounts.Accounts.FirstOrDefault(a => a.Id == e.AccountId);
                if(card is null)
                    return;

                card.SyncState = e.SyncState;
                Dashboard.UpdateAccountSyncState(e.AccountId, card);

                if(card.Id == Accounts.ActiveAccount?.Id)
                    SyncStatusBarToActiveAccount();
            });

    private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
        => Dispatcher.UIThread.Post(() =>
            {
                var card = Accounts.Accounts.FirstOrDefault(a => a.Id == e.Job.AccountId);

                string accountEmail = card?.Email ?? e.Job.AccountId;
                var item = ActivityItemViewModel.FromJob(e.Job, accountEmail);

                Activity.AddActivityItem(item);
                Dashboard.AddActivityItem(item);
            });

    private void OnConflictDetected(object? sender, SyncConflict conflict)
    {
        Activity.AddConflictItem(conflict);

        Dispatcher.UIThread.Post(() =>
        {
            var card = Accounts.Accounts.FirstOrDefault(a => a.Id == conflict.AccountId);
            if(card is not null)
            {
                card.ConflictCount++;
                Dashboard.UpdateAccountSyncState(conflict.AccountId, card);
            }

            SyncStatusBarToActiveAccount();
        });
    }

    private void SyncStatusBarToActiveAccount()
    {
        var active = Accounts.ActiveAccount;
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
