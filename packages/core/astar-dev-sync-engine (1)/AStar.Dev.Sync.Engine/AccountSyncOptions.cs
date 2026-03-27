namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Per-account sync configuration.
/// </summary>
public sealed class AccountSyncOptions
{
    /// <summary>Unique account identifier.</summary>
    public required string AccountId { get; init; }

    /// <summary>
    /// Maximum number of concurrent upload/download operations.
    /// Defaults to <see cref="SyncOptions.DefaultMaxConcurrency"/> when null.
    /// </summary>
    public int? MaxConcurrency { get; init; }

    /// <summary>
    /// Folder paths (relative, forward-slash separated) to include in sync.
    /// All files within these folders and their descendants are synced (SE-07).
    /// </summary>
    public required IReadOnlyList<string> SelectedFolders { get; init; }
}
