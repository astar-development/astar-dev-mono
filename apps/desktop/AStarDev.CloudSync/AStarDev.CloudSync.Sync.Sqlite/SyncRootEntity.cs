namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>The database row for an account's sync root.</summary>
internal sealed class SyncRootEntity
{
    /// <summary>CloudSync's id for the account.</summary>
    public Guid AccountId { get; set; }

    /// <summary>The root's normalised full path.</summary>
    public string Path { get; set; } = string.Empty;
}
