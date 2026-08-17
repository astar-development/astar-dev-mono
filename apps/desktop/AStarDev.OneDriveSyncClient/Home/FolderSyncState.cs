namespace AStarDev.OneDriveSyncClient.Home;

public enum FolderSyncState
{
    Excluded,
    Included,
    Partial,
    Syncing,
    Synced,
    Conflict,
    Error
}
