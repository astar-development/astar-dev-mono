namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a local item is a file or a folder.</summary>
public abstract record LocalItemKind
{
    private LocalItemKind()
    {
    }

    /// <summary>A file.</summary>
    public sealed record File : LocalItemKind
    {
        internal File()
        {
        }
    }

    /// <summary>A folder.</summary>
    public sealed record Folder : LocalItemKind
    {
        internal Folder()
        {
        }
    }
}
