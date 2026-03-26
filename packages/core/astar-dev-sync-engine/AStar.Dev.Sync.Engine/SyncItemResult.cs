namespace AStar.Dev.Sync.Engine;

/// <summary>
/// The outcome of syncing a single <see cref="SyncItem"/>.
/// </summary>
public sealed class SyncItemResult
{
    /// <summary>The item that was synced.</summary>
    public required SyncItem Item { get; init; }

    /// <summary>Whether the transfer succeeded.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Error message if the transfer failed; null on success.</summary>
    public string? ErrorMessage { get; init; }
}
