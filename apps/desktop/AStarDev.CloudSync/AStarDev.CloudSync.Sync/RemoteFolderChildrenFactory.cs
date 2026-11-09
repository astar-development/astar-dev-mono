namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteFolderChildren"/> cases.</summary>
public static class RemoteFolderChildrenFactory
{
    /// <summary>Creates the <see cref="RemoteFolderChildren.MayHaveChildFolders"/> case.</summary>
    public static RemoteFolderChildren CreateMayHaveChildFolders() => new RemoteFolderChildren.MayHaveChildFolders();

    /// <summary>Creates the <see cref="RemoteFolderChildren.NoChildFolders"/> case.</summary>
    public static RemoteFolderChildren CreateNoChildFolders() => new RemoteFolderChildren.NoChildFolders();

    /// <summary>Creates the case from Graph's <c>folder.childCount</c>.</summary>
    /// <param name="childCount">The number of children, files included.</param>
    public static RemoteFolderChildren CreateFromChildCount(int childCount) => childCount > 0 ? CreateMayHaveChildFolders() : CreateNoChildFolders();
}
