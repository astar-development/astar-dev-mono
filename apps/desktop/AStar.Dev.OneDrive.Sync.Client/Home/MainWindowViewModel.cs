using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountCardViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountCardViewModel;
using AccountsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountsViewModel;
using ActivityViewModel = AStar.Dev.OneDrive.Sync.Client.Activity.ActivityViewModel;
using DashboardViewModel = AStar.Dev.OneDrive.Sync.Client.Dashboard.DashboardViewModel;
using SettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Settings.SettingsViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class MainWindowViewModel(IAuthService authService, IGraphService graphService, IStartupService startupService, ISyncService syncService, IThemeService themeService,
    ISyncScheduler scheduler, ISyncRepository syncRepository, ISettingsService settingsService, IAccountRepository accountRepository, ILocalizationService localizationService) : ObservableObject
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

    public AccountsViewModel Accounts { get; } = new(authService, graphService, accountRepository);

    public FilesViewModel Files { get; } = new(authService, graphService, accountRepository);

    public ActivityViewModel Activity { get; } = new(syncService, syncRepository);

    public DashboardViewModel Dashboard { get; } = new(scheduler, localizationService, accountRepository);

    public SettingsViewModel Settings { get; } = new(settingsService, themeService, scheduler, accountRepository);

    public StatusBarViewModel StatusBar { get; } = new();

    public async Task InitialiseAsync()
    {
        try
        {
            syncService.SyncProgressChanged += OnSyncProgressChanged;
            syncService.JobCompleted += OnJobCompleted;
            syncService.ConflictDetected += OnConflictDetected;
            scheduler.SyncCompleted += OnSyncCompleted;

            Accounts.AccountSelected += OnAccountSelectedAsync;
            Accounts.AccountAdded += OnAccountAddedAsync;
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
        Accounts.AddAccount();
    }

    private async void OnAccountSelectedAsync(object? sender, AccountCardViewModel card)
        => await Try.RunAsync(async () =>
            {
                ActiveSection = NavSection.Files;
                await Files.ActivateAccountAsync(card.Id);
                await Activity.SetActiveAccountAsync(card.Id, card.Email);
                SyncStatusBarToActiveAccount();
                return Unit.Default;
            })
            .TapErrorAsync(e=> Serilog.Log.Error(e, "[MainWindowViewModel.OnAccountSelectedAsync] Error: {Error}", e));

    private async void OnAccountAddedAsync(object? sender, OneDriveAccount account)
        => await Try.RunAsync(async () =>
            {
                Files.AddAccount(account);
                Dashboard.AddAccount(account);
                Settings.AddAccount(account);
                ActiveSection = NavSection.Files;
                await Files.ActivateAccountAsync(account.Id);
                await Activity.SetActiveAccountAsync(account.Id, account.Email);
                return Unit.Default;
            })
            .TapErrorAsync(e=> Serilog.Log.Error(e, "[MainWindowViewModel.OnAccountAddedAsync] Error: {Error}", e));

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

                if(e.Total == 0 && !string.IsNullOrEmpty(e.CurrentFile))
                    Dashboard.AddActivityItem(new ActivityItemViewModel { AccountId = e.AccountId, FileName = e.CurrentFile, Type = ActivityItemType.Info });

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

    private void OnSyncCompleted(object? sender, string accountId)
        => Dispatcher.UIThread.Post(() =>
            {
                var card = Accounts.Accounts.FirstOrDefault(a => a.Id == accountId);
                card?.SyncState = SyncState.Completed;

                Dashboard.MarkSyncCompleted(accountId);
                SyncStatusBarToActiveAccount();
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
