namespace AStar.Dev.Sync.Engine.Features.StateTracking;

/// <summary>
///     Persists sync state and resume checkpoints to SQLite per account (EH-04, EH-05, EH-06).
///     All methods are thread-safe and accept a <see cref="CancellationToken"/> for clean shutdown.
/// </summary>
public interface ISyncStateStore
{
    /// <summary>Persists the current <paramref name="state"/> for <paramref name="accountId"/>.</summary>
    Task SetStateAsync(string accountId, SyncAccountState state, CancellationToken ct = default);

    /// <summary>Returns the last persisted state for <paramref name="accountId"/>, or <see langword="null"/> if none.</summary>
    Task<SyncAccountState?> GetStateAsync(string accountId, CancellationToken ct = default);

    /// <summary>Saves a resume checkpoint for the account identified by <paramref name="checkpoint"/>.</summary>
    Task SaveCheckpointAsync(SyncCheckpoint checkpoint, CancellationToken ct = default);

    /// <summary>Returns the most recent checkpoint for <paramref name="accountId"/>, or <see langword="null"/> if none.</summary>
    Task<SyncCheckpoint?> GetCheckpointAsync(string accountId, CancellationToken ct = default);

    /// <summary>Removes the checkpoint for <paramref name="accountId"/> after a successful sync.</summary>
    Task ClearCheckpointAsync(string accountId, CancellationToken ct = default);

    /// <summary>Persists the <paramref name="deltaToken"/> returned by the last successful sync for <paramref name="accountId"/> (SE-09).</summary>
    Task SaveDeltaTokenAsync(string accountId, string deltaToken, CancellationToken ct = default);

    /// <summary>Returns the stored delta token for <paramref name="accountId"/>, or <see langword="null"/> if none (SE-09).</summary>
    Task<string?> GetDeltaTokenAsync(string accountId, CancellationToken ct = default);
}
