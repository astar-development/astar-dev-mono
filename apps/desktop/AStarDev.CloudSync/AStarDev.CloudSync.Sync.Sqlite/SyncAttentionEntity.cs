namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class SyncAttentionEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string ItemKey { get; set; } = string.Empty;

    public bool IsConflict { get; set; }

    public string? Path { get; set; }

    public string? ConflictKind { get; set; }

    public bool LocalPresent { get; set; }

    public long LocalSize { get; set; }

    public DateTimeOffset? LocalModified { get; set; }

    public string? LocalHash { get; set; }

    public string? LocalTag { get; set; }

    public bool RemotePresent { get; set; }

    public long RemoteSize { get; set; }

    public DateTimeOffset? RemoteModified { get; set; }

    public string? RemoteHash { get; set; }

    public string? RemoteTag { get; set; }

    public Guid? PairId { get; set; }

    public string? Direction { get; set; }

    public DateTimeOffset DetectedAt { get; set; }

    public List<SyncAttentionPathEntity> HeldPaths { get; set; } = [];
}
