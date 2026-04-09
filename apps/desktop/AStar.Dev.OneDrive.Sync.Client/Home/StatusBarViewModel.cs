using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

/// <summary>
/// Drives the bottom status bar.  Bound to the active account only.
/// Updated by the sync engine via property changes.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string AccountDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AccountEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasAccount { get; set; }

    [ObservableProperty]
    public partial SyncState SyncState { get; set; } = SyncState.Idle;

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int ConflictCount { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    [ObservableProperty]
    public partial string StorageUsedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConflictPolicyText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SyncProgress { get; set; }

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
