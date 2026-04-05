using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

/// <summary>
/// Drives the bottom status bar.  Bound to the active account only.
/// Updated by the sync engine via property changes.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty] private string _accountDisplayName = string.Empty;
    [ObservableProperty] private string _accountEmail = string.Empty;
    [ObservableProperty] private bool _hasAccount;

    [ObservableProperty] private SyncState _syncState = SyncState.Idle;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _conflictCount;
    [ObservableProperty] private string _lastSyncText = string.Empty;
    [ObservableProperty] private bool _isSyncing;

    [ObservableProperty] private string _storageUsedText = string.Empty;

    [ObservableProperty] private string _conflictPolicyText = string.Empty;

    [ObservableProperty] private double _syncProgress;

    public string StatusLabel => SyncState switch
    {
        SyncState.Syncing => "Syncing ...",
        SyncState.Pending => $"{PendingCount} pending",
        SyncState.Conflict => ConflictCount == 1 ? "1 conflict" : $"{ConflictCount} conflicts",
        SyncState.Error => "Error",
        _ => "Synced"
    };

    partial void OnSyncStateChanged(SyncState value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnConflictCountChanged(int value) => OnPropertyChanged(nameof(StatusLabel));
}
