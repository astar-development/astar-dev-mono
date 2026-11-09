namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a local folder has sub-folders, so a tree view can show an expand arrow without loading the next level.</summary>
public abstract record LocalFolderChildren
{
    private LocalFolderChildren()
    {
    }

    /// <summary>The folder has at least one visible sub-folder.</summary>
    public sealed record HasChildFolders : LocalFolderChildren
    {
        internal HasChildFolders()
        {
        }
    }

    /// <summary>The folder has no visible sub-folders, or its sub-folders could not be read.</summary>
    public sealed record NoChildFolders : LocalFolderChildren
    {
        internal NoChildFolders()
        {
        }
    }
}
