using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Home;

public sealed record FolderTreeNode(Option<string> RemoteId, string Name, Option<string> ParentId, string AccountId, string RemotePath, FolderSyncState SyncState = FolderSyncState.Excluded, bool HasChildren = true);
