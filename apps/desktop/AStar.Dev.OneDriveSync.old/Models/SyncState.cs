namespace AStar.Dev.OneDriveSync.old.Models;

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
