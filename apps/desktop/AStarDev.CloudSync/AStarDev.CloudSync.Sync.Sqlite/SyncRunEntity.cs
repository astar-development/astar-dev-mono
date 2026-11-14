namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class SyncRunEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string Trigger { get; set; } = string.Empty;

    public long StartedAtUtcTicks { get; set; }

    public long? FinishedAtUtcTicks { get; set; }

    public string? Outcome { get; set; }

    public string? FailureReason { get; set; }
}
