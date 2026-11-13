namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>The database row for a selection rule.</summary>
internal sealed class SelectionRuleEntity
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

    /// <summary>The provider's id for the remote folder, when known.</summary>
    public string? RemoteId { get; set; }

    /// <summary>Whether the rule includes the folder; otherwise it excludes it.</summary>
    public bool IsInclude { get; set; }

    /// <summary>The direction of an include rule: TwoWay, UploadOnly or DownloadOnly.</summary>
    public string Direction { get; set; } = string.Empty;
}
