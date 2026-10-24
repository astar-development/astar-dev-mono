using System.Collections.ObjectModel;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStarDev.OneDriveSyncClient.Dashboard;

public sealed partial class DashboardViewModel(ILocalizationService localizationService, ISyncEventAggregator syncEventAggregator, IDashboardAccountViewModelFactory dashboardAccountViewModelFactory, IActivityItemViewModelFactory activityItemViewModelFactory, IUiTimer uiTimer) : LocalizedObservableObject(localizationService)
{
    public ObservableCollection<DashboardAccountViewModel> AccountSections { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    public partial int TotalAccounts { get; set; }

    [ObservableProperty]
    public partial int TotalFolders { get; set; }

    [ObservableProperty]
    public partial int TotalConflicts { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = localizationService.GetLocal("Common.Never");

    [ObservableProperty]
    public partial bool AnyErrors { get; set; }

    [ObservableProperty]
    public partial bool AnySyncing { get; set; }

    public bool HasAccounts => TotalAccounts > 0;

    public string OverallStatusText => (AnySyncing, AnyErrors, TotalConflicts) switch
    {
        (true, _, _) => Localization.GetLocal("StatusBar.Syncing"),
        (_, true, _) => Localization.GetLocal("StatusBar.Error"),
        (_, _, > 0) => TotalConflicts == 1
            ? Localization.GetLocal("StatusBar.Conflict", TotalConflicts)
            : Localization.GetLocal("StatusBar.Conflicts", TotalConflicts),
        _ => Localization.GetLocal("Dashboard.AllSynced")
    };

    /// <summary>Localised "accounts" stat label.</summary>
    public string AccountsLabelText => Localization.GetLocal("Dashboard.Accounts");

    /// <summary>Localised "folders synced" stat label.</summary>
    public string FoldersSyncedLabelText => Localization.GetLocal("Dashboard.FoldersSynced");

    /// <summary>Localised "conflicts" stat label.</summary>
    public string ConflictsLabelText => Localization.GetLocal("Dashboard.Conflicts");

    /// <summary>Localised "overall status" stat label.</summary>
    public string OverallStatusLabelText => Localization.GetLocal("Dashboard.OverallStatus");

    /// <summary>Localised "last sync" stat label.</summary>
    public string LastSyncLabelText => Localization.GetLocal("Dashboard.LastSync");

    /// <summary>Localised "No accounts connected" empty-state heading.</summary>
    public string NoAccountsConnectedText => Localization.GetLocal("Dashboard.NoAccountsConnected");

    /// <summary>Localised "Add an account to see your sync health here." empty-state detail.</summary>
    public string NoAccountsConnectedHintText => Localization.GetLocal("Dashboard.NoAccountsConnectedHint");

    public void SubscribeToSyncEvents()
    {
        syncEventAggregator.SyncProgressChanged += OnSyncProgressChanged;
        syncEventAggregator.JobCompleted += OnJobCompleted;
        syncEventAggregator.SyncCompleted += OnSyncCompleted;
        syncEventAggregator.ConflictDetected += OnConflictDetected;
        syncEventAggregator.ConflictResolved += OnConflictResolved;
    }

    /// <summary>Starts the periodic timer that refreshes relative timestamps on the dashboard. Call once during app initialisation.</summary>
    public void StartRefreshTimer() => uiTimer.Start(TimeSpan.FromMinutes(1), OnTimerTick);

    public void AddAccount(OneDriveAccount account)
    {
        if (AccountSections.Any(s => s.AccountId == account.Id.Value))
            return;

        var section = dashboardAccountViewModelFactory.Create(account);

        AccountSections.Add(section);

        RecalculateGlobals();
    }

    public void RemoveAccount(string accountId)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if (section is null)
            return;

        _ = AccountSections.Remove(section);

        RecalculateGlobals();
    }

    public void UpdateAccountSyncState(string accountId, AccountCardViewModel card)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if (section is null)
            return;

        section.UpdateSyncState(card.SyncState, card.ConflictCount);

        RecalculateGlobals();
    }

    public void UpdateFolderCount(string accountId, int folderCount)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if (section is null)
            return;

        section.FolderCount = folderCount;

        RecalculateGlobals();
    }

    /// <summary>Updates the storage quota for the named account section. No-op if the account is not present.</summary>
    public void UpdateQuota(string accountId, StorageQuota quota)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        section?.UpdateQuota(quota);
    }

    public void MarkSyncCompleted(string accountId)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if (section is null)
            return;

        section.UpdateSyncState(SyncState.Completed, section.ConflictCount);

        RecalculateGlobals();
    }

    public void AddActivityItem(ActivityItemViewModel item)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == item.AccountId);
        section?.AddRecentActivity(item);
    }

    private void OnTimerTick()
    {
        foreach (var section in AccountSections)
            section.RefreshTimeDisplays();

        RecalculateGlobals();
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs args)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == args.AccountId);
        if (section is null)
            return;

        section.UpdateSyncState(args.SyncState, section.ConflictCount);

        if (args.Total == 0 && !string.IsNullOrEmpty(args.CurrentFile))
            section.AddProgressActivity(args.CurrentFile);

        RecalculateGlobals();
    }

    private void OnJobCompleted(object? sender, JobCompletedEventArgs args)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == args.Job.Remote.AccountId.Value);
        string accountEmail = section?.Email ?? args.Job.Remote.AccountId.Value;
        var item = activityItemViewModelFactory.CreateFromJob(args.Job, accountEmail);
        AddActivityItem(item);
    }

    private void OnSyncCompleted(object? sender, string accountId) => MarkSyncCompleted(accountId);

    private void OnConflictDetected(object? sender, SyncConflict conflict)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == conflict.Remote.AccountId.Value);
        if (section is null)
            return;

        section.UpdateSyncState(section.SyncState, section.ConflictCount + 1);

        RecalculateGlobals();
    }

    private void OnConflictResolved(object? sender, SyncConflict conflict)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == conflict.Remote.AccountId.Value);
        if (section is null)
            return;

        section.UpdateSyncState(section.SyncState, Math.Max(0, section.ConflictCount - 1));

        RecalculateGlobals();
    }

    /// <inheritdoc />
    protected override void OnCultureChanged()
    {
        foreach (var section in AccountSections)
            section.RefreshTimeDisplays();

        RecalculateGlobals();
        base.OnCultureChanged();
    }

    private void RecalculateGlobals()
    {
        TotalAccounts = AccountSections.Count;
        TotalFolders = AccountSections.Sum(s => s.FolderCount);
        TotalConflicts = AccountSections.Sum(s => s.ConflictCount);
        AnyErrors = AccountSections.Any(s => s.SyncState == SyncState.Error);
        AnySyncing = AccountSections.Any(s => s.SyncState == SyncState.Syncing);

        var mostRecent = AccountSections.FirstOrDefault(s => s.HasEverSynced);

        LastSyncText = mostRecent?.LastSyncText ?? Localization.GetLocal("Common.Never");
        OnPropertyChanged(nameof(OverallStatusText));
    }
}
