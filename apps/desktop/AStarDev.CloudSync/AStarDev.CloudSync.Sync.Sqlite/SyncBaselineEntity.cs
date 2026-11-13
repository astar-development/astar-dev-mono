namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class SyncBaselineEntity
{
    public Guid AccountId { get; set; }

    public string Path { get; set; } = string.Empty;

    public string RemoteItemId { get; set; } = string.Empty;

    public bool IsFolder { get; set; }

    public long? Size { get; set; }

    public DateTimeOffset? LastWriteUtc { get; set; }

    public string? Hash { get; set; }

    public string? CTag { get; set; }

    public string? ETag { get; set; }
}
