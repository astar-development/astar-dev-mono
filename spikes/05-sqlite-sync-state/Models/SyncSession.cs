namespace AStar.Dev.Spikes.SqliteSyncState;

/// <summary>
/// Records a sync run for an account.
/// An InProgress session with no CompletedAt indicates an interrupted sync (RR-01).
/// </summary>
public class SyncSession
{
    public Guid   Id          { get; init; }
    public Guid   AccountId   { get; init; }
    public DateTimeOffset  StartedAt    { get; init; }
    public DateTimeOffset? CompletedAt  { get; set; } // null = in-progress or interrupted
    public int    ItemsSynced { get; set; }
    public SyncSessionStatus Status { get; set; } = SyncSessionStatus.InProgress;
    public string? ErrorMessage { get; set; }
}

public enum SyncSessionStatus
{
    InProgress, // SE-08: at most one InProgress per account at a time
    Completed,
    Failed,     // RR-03: user should be informed
}
