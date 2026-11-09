using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderTreePosition"/> cases.</summary>
public static class FolderTreePositionFactory
{
    /// <summary>Creates the <see cref="FolderTreePosition.Root"/> case.</summary>
    public static FolderTreePosition CreateRoot() => new FolderTreePosition.Root();

    /// <summary>Creates the <see cref="FolderTreePosition.Folder"/> case.</summary>
    public static FolderTreePosition CreateFolder(LocalRelativePath path, Option<RemoteItemId> remoteId) => new FolderTreePosition.Folder(path, remoteId);
}
