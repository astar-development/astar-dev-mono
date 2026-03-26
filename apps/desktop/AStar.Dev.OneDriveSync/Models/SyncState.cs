namespace AStar.Dev.OneDriveSync.Models;

public enum SyncState
{
    Idle,
    Synced,
    Syncing,
    Pending,
    Conflict,
    Excluded,
    Error
}
