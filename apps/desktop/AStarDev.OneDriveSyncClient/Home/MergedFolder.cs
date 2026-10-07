using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>A folder in the merged remote and local tree. <paramref name="RemoteId"/> is none until the folder exists on OneDrive.</summary>
public sealed record MergedFolder(string Name, string RemotePath, Option<string> RemoteId, Option<string> ParentId, FolderOrigin Origin, FolderSyncState SyncState);
