namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderMove"/> values.</summary>
public static class FolderMoveFactory
{
    /// <summary>Creates the move.</summary>
    public static FolderMove Create(LocalRelativePath oldPath, LocalRelativePath newPath) => new(oldPath, newPath);
}
