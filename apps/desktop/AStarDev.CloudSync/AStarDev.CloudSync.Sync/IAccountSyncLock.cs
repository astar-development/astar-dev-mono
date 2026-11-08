using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Allows one sync pass per account at a time, and lets an update or an account removal wait for every running pass.</summary>
public interface IAccountSyncLock
{
    /// <summary>Starts a pass for the account if none is running and the lock is not drained. Never waits.</summary>
    /// <param name="accountId">The account to sync.</param>
    SyncLockAttempt TryAcquire(AccountId accountId);

    /// <summary>Stops new passes from starting and waits until every running pass has released its lease.</summary>
    /// <param name="cancellationToken">Cancels the wait; the lock reopens and the call throws <see cref="OperationCanceledException"/>.</param>
    /// <returns>The drain; dispose it to let passes start again.</returns>
    Task<SyncDrain> DrainAsync(CancellationToken cancellationToken);
}
