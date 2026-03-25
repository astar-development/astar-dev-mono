namespace AStar.Dev.Spikes.SqliteSyncState;

/// <summary>
/// Stores the Microsoft Graph delta link for a given account and folder.
/// Used to perform incremental sync (only changed files since last sync).
/// </summary>
public class SyncDeltaToken
{
    public Guid   Id         { get; init; }
    public Guid   AccountId  { get; init; }

    /// <summary>OneDrive folder path, e.g. "/" for root or "/Documents".</summary>
    public string FolderPath { get; init; } = string.Empty;

    /// <summary>The full delta link URL returned by Microsoft Graph.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
