using AStar.Dev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Home;

public sealed record FolderTreeNode(string Id, string Name, Option<string> ParentId, string AccountId, string RemotePath, FolderSyncState SyncState = FolderSyncState.Excluded, bool HasChildren = true);
