namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class RemoteItemEntity
{
    public Guid AccountId { get; set; }

    public string RemoteItemId { get; set; } = string.Empty;

    public string? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsFolder { get; set; }

    public long? Size { get; set; }

    public string? Hash { get; set; }

    public string? CTag { get; set; }

    public string ETag { get; set; } = string.Empty;

    public DateTimeOffset LastModified { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? LastSeenPass { get; set; }
}
