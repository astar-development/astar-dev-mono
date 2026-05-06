namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed record FolderTreeNode(
    string Id,
    string Name,
    string? ParentId,
    string AccountId,
    string RemotePath,
    FolderSyncState SyncState = FolderSyncState.Excluded,
    bool HasChildren = true);
