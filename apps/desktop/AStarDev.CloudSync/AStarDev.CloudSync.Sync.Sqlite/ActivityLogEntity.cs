namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class ActivityLogEntity
{
    public long Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid? SyncPairId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public int FileCount { get; set; }

    public long Bytes { get; set; }

    public string? FileName { get; set; }

    public long OccurredAtUtcTicks { get; set; }
}
