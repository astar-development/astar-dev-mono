namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a remote folder may have child folders. Mirrors LocalFolderChildren (#1395).</summary>
public abstract record RemoteFolderChildren
{
    private RemoteFolderChildren()
    {
    }

    /// <summary>Graph's <c>folder.childCount &gt; 0</c>. The count includes files, so expanding may show nothing.</summary>
    public sealed record MayHaveChildFolders : RemoteFolderChildren
    {
        internal MayHaveChildFolders()
        {
        }
    }

    /// <summary><c>childCount == 0</c>.</summary>
    public sealed record NoChildFolders : RemoteFolderChildren
    {
        internal NoChildFolders()
        {
        }
    }
}
