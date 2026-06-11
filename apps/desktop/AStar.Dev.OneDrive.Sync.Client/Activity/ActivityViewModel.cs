using System.Collections.ObjectModel;
using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Activity;

public sealed partial class ActivityViewModel(ISyncRepository syncRepository, ISyncEventAggregator syncEventAggregator, IConflictItemViewModelFactory conflictItemViewModelFactory, IActivityItemViewModelFactory activityItemViewModelFactory, IUiDispatcher dispatcher, ILocalizationService loc) : ObservableObject
{
    private const int MaxLogSize = 10_000;
    private string? activeAccountId;
    private string activeAccountEmail = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLogTabActive))]
    [NotifyPropertyChangedFor(nameof(IsConflictsTabActive))]
    public partial ActivityTab ActiveTab { get; set; } = ActivityTab.Log;

    public bool IsLogTabActive => ActiveTab == ActivityTab.Log;
    public bool IsConflictsTabActive => ActiveTab == ActivityTab.Conflicts;

    [RelayCommand]
    private void SwitchTab(ActivityTab tab) => ActiveTab = tab;

    public ObservableCollection<ActivityItemViewModel> LogItems { get; } = [];
    public ObservableCollection<ActivityItemViewModel> FilteredLog { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogItems))]
    public partial int LogItemCount { get; set; }

    public bool HasLogItems => LogItemCount > 0;

    [ObservableProperty]
    public partial ActivityItemType? ActiveFilter { get; set; }
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflicts))]
    [NotifyPropertyChangedFor(nameof(ConflictBadgeText))]
    public partial int ConflictCount { get; set; }

    public bool HasConflicts => ConflictCount > 0;
    public string ConflictBadgeText => ConflictCount > 0 ? ConflictCount.ToString(CultureInfo.CurrentCulture) : string.Empty;

    /// <summary>Localised "Activity log" tab label.</summary>
    public string LogTabText => loc.GetLocal("Activity.LogTab");

    /// <summary>Localised "Conflicts" tab label.</summary>
    public string ConflictsTabText => loc.GetLocal("Activity.ConflictsTab");

    /// <summary>Localised "All" filter chip label.</summary>
    public string FilterAllText => loc.GetLocal("Activity.Filter.All");

    /// <summary>Localised "↓ downloads" filter chip label.</summary>
    public string FilterDownloadsText => loc.GetLocal("Activity.Filter.Downloads");

    /// <summary>Localised "↑ uploads" filter chip label.</summary>
    public string FilterUploadsText => loc.GetLocal("Activity.Filter.Uploads");

    /// <summary>Localised "⚠ errors" filter chip label.</summary>
    public string FilterErrorsText => loc.GetLocal("Activity.Filter.Errors");

    /// <summary>Localised "Clear" filter chip label.</summary>
    public string FilterClearText => loc.GetLocal("Activity.Filter.Clear");

    /// <summary>Localised "No activity yet" empty state heading.</summary>
    public string NoActivityYetText => loc.GetLocal("Activity.NoActivityYet");

    /// <summary>Localised "Sync activity will appear here." empty state detail.</summary>
    public string SyncActivityWillAppearHereText => loc.GetLocal("Activity.SyncActivityWillAppearHere");

    /// <summary>Localised "No conflicts" empty state heading.</summary>
    public string NoConflictsText => loc.GetLocal("Activity.NoConflicts");

    /// <summary>Localised "Any sync conflicts will appear here for resolution." empty state detail.</summary>
    public string NoConflictsHintText => loc.GetLocal("Activity.NoConflictsHint");

    /// <summary>Localised "resolved" badge label.</summary>
    public string ResolvedText => loc.GetLocal("Activity.Conflict.Resolved");

    public void SubscribeToSyncEvents()
    {
        syncEventAggregator.SyncProgressChanged += OnSyncProgressChanged;
        syncEventAggregator.JobCompleted += OnJobCompleted;
        syncEventAggregator.ConflictDetected += OnConflictDetected;
    }

    /// <summary>
    /// Called by MainWindowViewModel when the active account changes.
    /// Loads persisted conflicts for the account.
    /// </summary>
    public async Task SetActiveAccountAsync(string accountId, string accountEmail)
    {
        activeAccountId = accountId;
        activeAccountEmail = accountEmail;

        Conflicts.Clear();
        FilteredLog.Clear();

        await LoadPersistedConflictsAsync(accountId);

        RebuildFilteredLog();
        ConflictCount = Conflicts.Count;
    }

    private async Task LoadPersistedConflictsAsync(string accountId)
    {
        var persistedConflicts = await syncRepository.GetPendingConflictsAsync(new AccountId(accountId));

        foreach(var entity in persistedConflicts)
        {
            var model = MapConflictEntityToViewModel(entity);
            AddConflict(model);
        }
    }

    private static SyncConflict MapConflictEntityToViewModel(SyncConflictEntity entity)
        => new()
        {
            Id         = entity.Id,
            Remote     = RemoteItemRefFactory.Create(entity.AccountId, entity.FolderId, entity.RemoteItemId),
            Target     = SyncFileTargetFactory.Create(entity.LocalPath, entity.RelativePath),
            Snapshot   = ConflictSnapshotFactory.Create(entity.LocalModified, entity.LocalSize, entity.RemoteModified, entity.RemoteSize),
            DetectedAt = entity.DetectedAt
        };

    /// <summary>Called when a sync job completes.</summary>
    public void AddActivityItem(ActivityItemViewModel item) => dispatcher.Post(() =>
                                                                    {
                                                                        LogItems.Insert(0, item);

                                                                        while(LogItems.Count > MaxLogSize)
                                                                            LogItems.RemoveAt(LogItems.Count - 1);

                                                                        LogItemCount = LogItems.Count;
                                                                        RebuildFilteredLog();
                                                                    });

    /// <summary>Called when a new conflict is detected.</summary>
    public void AddConflictItem(SyncConflict conflict) => dispatcher.Post(() =>
                                                               {
                                                                   if(Conflicts.Any(c => c.Id == conflict.Id))
                                                                       return;
                                                                   AddConflict(conflict);
                                                                   ConflictCount = Conflicts.Count;

                                                                   ActiveTab = ActivityTab.Conflicts;
                                                               });

    [RelayCommand]
    private void SetFilter(ActivityItemType? filter)
    {
        ActiveFilter = filter;
        RebuildFilteredLog();
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogItems.Clear();
        FilteredLog.Clear();
        LogItemCount = 0;
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs args)
    {
        if(args.Total != 0 || string.IsNullOrEmpty(args.CurrentFile))
            return;

        var item = activityItemViewModelFactory.CreateInfo(args.AccountId, args.CurrentFile);
        AddActivityItem(item);
    }

    private void OnJobCompleted(object? sender, JobCompletedEventArgs args)
    {
        var item = activityItemViewModelFactory.CreateFromJob(args.Job, _activeAccountEmail);
        AddActivityItem(item);
    }

    private void OnConflictDetected(object? sender, SyncConflict conflict) => AddConflictItem(conflict);

    private void AddConflict(SyncConflict conflict)
    {
        var vm = conflictItemViewModelFactory.Create(conflict);
        vm.Resolved += (_, conflictItem) =>
        {
            _ = Conflicts.Remove(conflictItem);
            ConflictCount = Conflicts.Count;
        };
        Conflicts.Add(vm);
    }

    private void RebuildFilteredLog()
    {
        FilteredLog.Clear();

        var query = LogItems
            .Where(i => activeAccountId is null || i.AccountId == activeAccountId);

        if(ActiveFilter.HasValue)
            query = query.Where(i => i.Type == ActiveFilter.Value);

        foreach(var item in query)
            FilteredLog.Add(item);

        LogItemCount = FilteredLog.Count;
    }
}
