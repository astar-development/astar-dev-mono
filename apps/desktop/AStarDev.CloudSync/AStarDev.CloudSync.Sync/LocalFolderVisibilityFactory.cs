namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalFolderVisibility"/> cases.</summary>
public static class LocalFolderVisibilityFactory
{
    /// <summary>Creates the <see cref="LocalFolderVisibility.Visible"/> case.</summary>
    public static LocalFolderVisibility CreateVisible() => new LocalFolderVisibility.Visible();

    /// <summary>Creates the <see cref="LocalFolderVisibility.Skipped"/> case.</summary>
    public static LocalFolderVisibility CreateSkipped(SkipReason reason) => new LocalFolderVisibility.Skipped(reason);
}
