using System.Collections.Concurrent;

namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Thread-safe per-account sync lock (SE-08).
/// </summary>
public sealed class SyncGate : ISyncLock
{
    private readonly ConcurrentDictionary<string, byte> _running = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool TryAcquire(string accountId) => _running.TryAdd(accountId, 0);

    /// <inheritdoc />
    public void Release(string accountId) => _running.TryRemove(accountId, out _);

    /// <inheritdoc />
    public bool IsRunning(string accountId) => _running.ContainsKey(accountId);
}
