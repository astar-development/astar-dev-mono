using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Creates <see cref="MergedFolder"/> instances.</summary>
public static class MergedFolderFactory
{
    /// <summary>Creates a merged folder from its resolved values.</summary>
    public static MergedFolder Create(string name, string remotePath, Option<string> remoteId, Option<string> parentId, FolderOrigin origin, FolderSyncState syncState) => new(name, remotePath, remoteId, parentId, origin, syncState);
}
