namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalFolderChildren"/> cases.</summary>
public static class LocalFolderChildrenFactory
{
    /// <summary>Creates the <see cref="LocalFolderChildren.HasChildFolders"/> case.</summary>
    public static LocalFolderChildren CreateHasChildFolders() => new LocalFolderChildren.HasChildFolders();

    /// <summary>Creates the <see cref="LocalFolderChildren.NoChildFolders"/> case.</summary>
    public static LocalFolderChildren CreateNoChildFolders() => new LocalFolderChildren.NoChildFolders();
}
