using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Allows one sync pass per account at a time, and lets an update or an account removal wait for every running pass.</summary>
public interface IAccountSyncLock
{
    /// <summary>Starts a pass for the account if none is running and the lock is not drained. Never waits, not even behind a background lease; passes and user actions use <see cref="AcquireAsync"/> instead.</summary>
    /// <param name="accountId">The account to sync.</param>
    SyncLockAttempt TryAcquire(AccountId accountId);

    /// <summary>Takes the account's lease for a sync pass or a user action. When only background work (a reclassify write) holds it, waits for that short write to end, at most <see cref="AccountLeasePolicy.WaitForRunningWork"/>, and no new background lease starts meanwhile. Refuses at once when a pass holds the lease or the lock is drained. A cancel ends the wait and returns the refusal.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Ends the wait.</param>
    Task<SyncLockAttempt> AcquireAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Takes the account's lease for low-priority background work, such as a reclassify write, that gives way to passes and user actions. Refused while any lease is held or a caller of <see cref="AcquireAsync"/> is waiting. Never waits.</summary>
    /// <param name="accountId">The account.</param>
    SyncLockAttempt TryAcquireBackground(AccountId accountId);

    /// <summary>Stops new passes from starting and waits until every running pass has released its lease. A pass that never disposes its lease makes this wait forever, so callers pass a token with a timeout.</summary>
    /// <param name="cancellationToken">Cancels the wait; the lock reopens and the call throws <see cref="OperationCanceledException"/>.</param>
    /// <returns>The drain; dispose it to let passes start again.</returns>
    Task<SyncDrain> DrainAsync(CancellationToken cancellationToken);
}
