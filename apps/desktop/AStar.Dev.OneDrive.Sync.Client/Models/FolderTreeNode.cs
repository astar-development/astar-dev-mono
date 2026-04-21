namespace AStar.Dev.OneDrive.Sync.Client.Models;

public enum FolderSyncState
{
    Excluded,
    Included,
    Partial,    // some children included, some excluded
    Syncing,
    Synced,
    Conflict,
    Error
}

public sealed record FolderTreeNode(
    string Id,
    string Name,
    string? ParentId,
    string AccountId,
    string RemotePath,
    FolderSyncState SyncState = FolderSyncState.Excluded,
    bool HasChildren = true);
