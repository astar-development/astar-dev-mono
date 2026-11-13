namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class SyncBaselineEntity
{
    public Guid AccountId { get; set; }

    private string path = string.Empty;

    /// <summary>The path as given, kept for display and for reading back.</summary>
    public string Path
    {
        get => path;
        set
        {
            path = value;
            PathKey = value.ToUpperInvariant();
        }
    }

    /// <summary>The path folded to upper case with the invariant culture: the key that matches paths ignoring case, non-ASCII letters included. SQLite NOCASE folds ASCII only (#1536). It follows <see cref="Path"/> and is never set on its own.</summary>
    public string PathKey { get; set; } = string.Empty;

    public string RemoteItemId { get; set; } = string.Empty;

    public bool IsFolder { get; set; }

    public long? Size { get; set; }

    public DateTimeOffset? LastWriteUtc { get; set; }

    public string? Hash { get; set; }

    public string? CTag { get; set; }

    public string? ETag { get; set; }
}
