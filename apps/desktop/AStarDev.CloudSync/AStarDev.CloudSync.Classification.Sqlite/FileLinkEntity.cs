namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class FileLinkEntity
{
    private string path = string.Empty;

    public Guid AccountId { get; set; }

    /// <summary>The path as given, kept for reading back.</summary>
    public string Path
    {
        get => path;
        set
        {
            path = value;
            PathKey = value.ToUpperInvariant();
        }
    }

    /// <summary>The path folded to upper case with the invariant culture: the key that matches paths ignoring case, non-ASCII letters included (#1536). It follows <see cref="Path"/> and is never set on its own.</summary>
    public string PathKey { get; set; } = string.Empty;

    public Guid TermId { get; set; }

    public string Origin { get; set; } = string.Empty;
}
