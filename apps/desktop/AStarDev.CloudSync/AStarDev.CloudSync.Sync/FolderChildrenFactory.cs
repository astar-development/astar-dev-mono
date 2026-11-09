namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderChildren"/> cases.</summary>
public static class FolderChildrenFactory
{
    /// <summary>Creates the <see cref="FolderChildren.MayHaveChildFolders"/> case.</summary>
    public static FolderChildren CreateMayHaveChildFolders() => new FolderChildren.MayHaveChildFolders();

    /// <summary>Creates the <see cref="FolderChildren.NoChildFolders"/> case.</summary>
    public static FolderChildren CreateNoChildFolders() => new FolderChildren.NoChildFolders();
}
