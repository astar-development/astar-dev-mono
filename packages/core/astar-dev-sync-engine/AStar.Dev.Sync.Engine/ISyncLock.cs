namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Per-account concurrency gate ensuring only one sync runs at a time (SE-08).
/// </summary>
public interface ISyncLock
{
    /// <summary>
    /// Attempts to acquire the sync lock for the given account.
    /// Returns true if the lock was acquired; false if a sync is already running.
    /// </summary>
    /// <param name="accountId">The account to lock.</param>
    bool TryAcquire(string accountId);

    /// <summary>
    /// Releases the sync lock for the given account.
    /// </summary>
    /// <param name="accountId">The account to unlock.</param>
    void Release(string accountId);

    /// <summary>
    /// Returns whether a sync is currently running for the given account.
    /// </summary>
    /// <param name="accountId">The account to check.</param>
    bool IsRunning(string accountId);
}
