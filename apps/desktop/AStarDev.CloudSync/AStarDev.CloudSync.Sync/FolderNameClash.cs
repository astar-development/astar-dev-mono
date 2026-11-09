namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a folder name is unique on disk (#1397).</summary>
public abstract record FolderNameClash
{
    private FolderNameClash()
    {
    }

    /// <summary>No other local folder has the same name ignoring case.</summary>
    public sealed record Unique : FolderNameClash
    {
        internal Unique()
        {
        }
    }

    /// <summary>Two local folders differ only by case, so they cannot sync to a cloud that ignores case. The node cannot be included until one is renamed.</summary>
    public sealed record CaseClash : FolderNameClash
    {
        internal CaseClash()
        {
        }
    }
}
