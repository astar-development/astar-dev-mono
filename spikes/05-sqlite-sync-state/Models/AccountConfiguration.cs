namespace AStar.Dev.Spikes.SqliteSyncState;

/// <summary>Per-account settings stored in the local database.</summary>
public class AccountConfiguration
{
    public Guid   Id                  { get; init; }
    public string DisplayName         { get; init; } = string.Empty;
    public string LocalSyncPath       { get; init; } = string.Empty;
    public int    SyncIntervalMinutes { get; init; } = 60;
    public int    MaxConcurrency      { get; init; } = 8;
    public bool   VerboseLogging      { get; init; }

    /// <summary>
    /// When the next scheduled sync should fire.
    /// Set to a staggered value when the account is first added (SE-05).
    /// </summary>
    public DateTimeOffset NextSyncAt { get; set; }
}
