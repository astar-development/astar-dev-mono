namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalFolder"/> values.</summary>
public static class LocalFolderFactory
{
    /// <summary>Creates a listed folder. A missing name becomes empty.</summary>
    /// <param name="name">The folder's name.</param>
    /// <param name="path">The folder's path relative to the sync root.</param>
    /// <param name="children">Whether the folder has sub-folders.</param>
    public static LocalFolder Create(string? name, LocalRelativePath path, LocalFolderChildren children) => new(name ?? string.Empty, path, children);
}
