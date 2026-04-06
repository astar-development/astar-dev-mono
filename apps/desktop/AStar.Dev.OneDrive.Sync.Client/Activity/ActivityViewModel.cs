using System.Collections.ObjectModel;
using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class ActivityViewModel(ISyncService syncService, ISyncRepository syncRepository) : ObservableObject
{
    private const int MaxLogSixe = 10_000;
    private string? _activeAccountId;
    private string _activeAccountEmail = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLogTabActive))]
    [NotifyPropertyChangedFor(nameof(IsConflictsTabActive))]
    private ActivityTab _activeTab = ActivityTab.Log;

    public bool IsLogTabActive => ActiveTab == ActivityTab.Log;
    public bool IsConflictsTabActive => ActiveTab == ActivityTab.Conflicts;

    [RelayCommand]
    private void SwitchTab(ActivityTab tab) => ActiveTab = tab;

    public ObservableCollection<ActivityItemViewModel> LogItems { get; } = [];
    public ObservableCollection<ActivityItemViewModel> FilteredLog { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogItems))]
    private int _logItemCount;

    public bool HasLogItems => LogItemCount > 0;

    [ObservableProperty] private ActivityItemType? _activeFilter;

    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflicts))]
    [NotifyPropertyChangedFor(nameof(ConflictBadgeText))]
    private int _conflictCount;

    public bool HasConflicts => ConflictCount > 0;
    public string ConflictBadgeText => ConflictCount > 0 ? ConflictCount.ToString(CultureInfo.CurrentCulture) : string.Empty;

    /// <summary>
    /// Called by MainWindowViewModel when the active account changes.
    /// Loads persisted conflicts for the account.
    /// </summary>
    public async Task SetActiveAccountAsync(string accountId, string accountEmail)
    {
        _activeAccountId = accountId;
        _activeAccountEmail = accountEmail;

        Conflicts.Clear();
        FilteredLog.Clear();

        var persistedConflicts = await syncRepository.GetPendingConflictsAsync(accountId);

        foreach(var entity in persistedConflicts)
        {
            var model = new SyncConflict
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                FolderId = entity.FolderId,
                RemoteItemId = entity.RemoteItemId,
                RelativePath = entity.RelativePath,
                LocalPath = entity.LocalPath,
                LocalModified = entity.LocalModified,
                RemoteModified = entity.RemoteModified,
                LocalSize = entity.LocalSize,
                RemoteSize = entity.RemoteSize,
                DetectedAt = entity.DetectedAt
            };

            AddConflict(model);
        }

        RebuildFilteredLog();
        ConflictCount = Conflicts.Count;
    }

    /// <summary>Called by MainWindowViewModel when a sync job completes.</summary>
    public void AddActivityItem(ActivityItemViewModel item) => Dispatcher.UIThread.Post(() =>
                                                                    {
                                                                        LogItems.Insert(0, item);

                                                                        while(LogItems.Count > MaxLogSixe)
                                                                            LogItems.RemoveAt(LogItems.Count - 1);

                                                                        LogItemCount = LogItems.Count;
                                                                        RebuildFilteredLog();
                                                                    });

    /// <summary>Called by MainWindowViewModel when a new conflict is detected.</summary>
    public void AddConflictItem(SyncConflict conflict) => Dispatcher.UIThread.Post(() =>
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

    private void AddConflict(SyncConflict conflict)
    {
        var vm = new ConflictItemViewModel(conflict, syncService);
        vm.Resolved += (_, c) =>
        {
            _ = Conflicts.Remove(c);
            ConflictCount = Conflicts.Count;
        };
        Conflicts.Add(vm);
    }

    private void RebuildFilteredLog()
    {
        FilteredLog.Clear();

        var query = LogItems
            .Where(i => _activeAccountId is null || i.AccountId == _activeAccountId);

        if(ActiveFilter.HasValue)
            query = query.Where(i => i.Type == ActiveFilter.Value);

        foreach(var item in query)
            FilteredLog.Add(item);

        LogItemCount = FilteredLog.Count;
    }
}
