using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderTreeNode"/> values.</summary>
public static class FolderTreeNodeFactory
{
    /// <summary>Creates a tree node. A missing name becomes empty.</summary>
    /// <param name="name">The folder's name.</param>
    /// <param name="path">The folder's path relative to the sync root.</param>
    /// <param name="remoteId">The cloud folder's id, when it exists.</param>
    /// <param name="origin">Where the folder exists.</param>
    /// <param name="children">Whether it may have child folders.</param>
    /// <param name="selection">What the selection rules say about it.</param>
    /// <param name="nameClash">Whether its name clashes with another local folder.</param>
    public static FolderTreeNode Create(string? name, LocalRelativePath path, Option<RemoteItemId> remoteId, FolderOrigin origin, FolderChildren children, SelectionState selection, FolderNameClash nameClash) =>
        new(name ?? string.Empty, path, remoteId, origin, children, selection, nameClash);
}
