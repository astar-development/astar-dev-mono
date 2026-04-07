using System.Collections.Concurrent;

namespace AStar.Dev.Sync.Engine.Features.Concurrency;

/// <summary>
///     Enforces: max one active sync per account (SE-02, SE-06) and manages the per-account transfer-slot
///     <see cref="SemaphoreSlim"/> (configurable 1–10, default 5, hard ceiling 10).
/// </summary>
public sealed class SyncGate : IDisposable
{
    private const int DefaultMaxTransfers = 5;
    private const int MaxTransferCeiling = 10;
    private const int MinTransfers = 1;

    private readonly ConcurrentDictionary<string, byte> _activeSyncs = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _transferSlots = new();
    private readonly int _maxConcurrentTransfers;

    /// <summary>Creates a <see cref="SyncGate"/> with the configured max concurrent transfers per account.</summary>
    /// <param name="maxConcurrentTransfersPerAccount">Must be in range 1–10; values outside are clamped.</param>
    public SyncGate(int maxConcurrentTransfersPerAccount = DefaultMaxTransfers) => _maxConcurrentTransfers = Math.Clamp(maxConcurrentTransfersPerAccount, MinTransfers, MaxTransferCeiling);

    /// <summary>
    ///     Attempts to register <paramref name="accountId"/> as actively syncing.
    /// </summary>
    /// <returns><see langword="true"/> if the slot was acquired; <see langword="false"/> if that account is already syncing.</returns>
    public bool TryBeginSync(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return _activeSyncs.TryAdd(accountId, 0);
    }

    /// <summary>Releases the active-sync slot for <paramref name="accountId"/>.</summary>
    public void EndSync(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        _activeSyncs.TryRemove(accountId, out _);
    }

    /// <summary>Returns <see langword="true"/> if at least one account is currently syncing.</summary>
    public bool IsAnyAccountSyncing() => !_activeSyncs.IsEmpty;

    /// <summary>Returns the <see cref="SemaphoreSlim"/> governing concurrent file transfers for <paramref name="accountId"/>.</summary>
    public SemaphoreSlim GetTransferSlots(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return _transferSlots.GetOrAdd(accountId, _ => new SemaphoreSlim(_maxConcurrentTransfers, _maxConcurrentTransfers));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var semaphore in _transferSlots.Values)
            semaphore.Dispose();
    }
}
