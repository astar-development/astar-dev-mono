using AStarDev.OneDriveSyncClient.Accounts;

namespace AStarDev.OneDriveSyncClient.Dashboard;

/// <summary>Keeps the synced file count on each dashboard account card up to date.</summary>
public interface IDashboardFileCountRefresher
{
    /// <summary>Subscribes to sync completion so counts refresh after every sync.</summary>
    void SubscribeToSyncEvents();

    /// <summary>Loads and publishes the current synced file count for the supplied account. Failures are logged and leave the previous count in place.</summary>
    Task RefreshAsync(OneDriveAccount account, CancellationToken cancellationToken = default);
}
