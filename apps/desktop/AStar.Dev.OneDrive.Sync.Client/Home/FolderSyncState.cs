namespace AStar.Dev.OneDrive.Sync.Client.Home;

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
