namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Summary of a completed sync run for a single account.
/// </summary>
public sealed class SyncReport
{
    /// <summary>The account that was synced.</summary>
    public required string AccountId { get; init; }

    /// <summary>UTC timestamp when the sync started.</summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>UTC timestamp when the sync completed.</summary>
    public required DateTimeOffset CompletedAtUtc { get; init; }

    /// <summary>Individual results for each item that was processed.</summary>
    public required IReadOnlyList<SyncItemResult> ItemResults { get; init; }

    /// <summary>Number of items that were uploaded (local to remote).</summary>
    public int Uploaded => ItemResults.Count(r => r.Succeeded && r.Item.Direction is SyncDirection.LocalToRemote);

    /// <summary>Number of items that were downloaded (remote to local).</summary>
    public int Downloaded => ItemResults.Count(r => r.Succeeded && r.Item.Direction is SyncDirection.RemoteToLocal);

    /// <summary>Number of items that failed to sync.</summary>
    public int Failed => ItemResults.Count(r => !r.Succeeded);
}
