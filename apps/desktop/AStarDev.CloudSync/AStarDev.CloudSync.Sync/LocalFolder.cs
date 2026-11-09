namespace AStarDev.CloudSync.Sync;

/// <summary>A folder under an account's sync root, as listed for the folder tree.</summary>
public sealed record LocalFolder
{
    internal LocalFolder(string name, LocalRelativePath path, LocalFolderChildren children)
    {
        Name = name;
        Path = path;
        Children = children;
    }

    /// <summary>The folder's name.</summary>
    public string Name { get; }

    /// <summary>The folder's path relative to the sync root.</summary>
    public LocalRelativePath Path { get; }

    /// <summary>Whether the folder has sub-folders to show.</summary>
    public LocalFolderChildren Children { get; }
}
