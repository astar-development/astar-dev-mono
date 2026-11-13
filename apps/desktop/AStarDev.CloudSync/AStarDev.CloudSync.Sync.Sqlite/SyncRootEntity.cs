namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>The database row for an account's sync root.</summary>
internal sealed class SyncRootEntity
{
    /// <summary>CloudSync's id for the account.</summary>
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
}
