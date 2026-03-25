namespace AStar.Dev.Spikes.SqliteSyncState;

/// <summary>
/// A sync conflict that requires user resolution (CR-01 through CR-08).
/// Persists across sessions — skipped conflicts remain here indefinitely (CR-05).
/// </summary>
public class ConflictQueueItem
{
    public Guid   Id           { get; init; }
    public Guid   AccountId    { get; init; }

    /// <summary>Path of the file in OneDrive (remote path).</summary>
    public string RemotePath   { get; init; } = string.Empty;

    /// <summary>Absolute path of the file on the local file system.</summary>
    public string LocalPath    { get; init; } = string.Empty;

    public ConflictType       ConflictType { get; init; }
    public ConflictResolution Resolution   { get; set; } = ConflictResolution.Pending;
    public DateTimeOffset     DetectedAt   { get; init; }
    public DateTimeOffset?    ResolvedAt   { get; set; }
}

public enum ConflictType
{
    BothModified,    // file changed on both sides (CR-01a)
    DeletedOnLocal,  // deleted locally, still present/modified remotely (CR-01b)
    DeletedOnRemote, // deleted remotely, still present/modified locally (CR-01b)
}

public enum ConflictResolution
{
    Pending,     // user has not resolved yet (CR-05: skippable indefinitely)
    LocalWins,   // CR-03
    RemoteWins,  // CR-03
    KeepBoth,    // CR-03 / CR-04: duplicate renamed with datetime suffix
    Skipped,     // user explicitly skipped without choosing a strategy
}
