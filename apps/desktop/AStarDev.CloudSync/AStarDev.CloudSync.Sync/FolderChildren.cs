namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a tree folder may have child folders, from both sides (#1397). Mirrors LocalFolderChildren and RemoteFolderChildren.</summary>
public abstract record FolderChildren
{
    private FolderChildren()
    {
    }

    /// <summary>At least one side reports child folders. The cloud count includes files, so expanding may show nothing.</summary>
    public sealed record MayHaveChildFolders : FolderChildren
    {
        internal MayHaveChildFolders()
        {
        }
    }

    /// <summary>Neither side reports child folders.</summary>
    public sealed record NoChildFolders : FolderChildren
    {
        internal NoChildFolders()
        {
        }
    }
}
