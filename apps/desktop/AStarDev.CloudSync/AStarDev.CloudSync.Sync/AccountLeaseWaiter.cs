using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Takes an account's sync lease, waiting up to <see cref="AccountLeasePolicy.WaitForRunningWork"/> for running work to finish.</summary>
/// <param name="syncLock">The account lock.</param>
/// <param name="timeProvider">Times the wait.</param>
internal sealed class AccountLeaseWaiter(IAccountSyncLock syncLock, TimeProvider timeProvider)
{
    /// <summary>Takes the lease.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The lease, or none when the account stayed busy or the lock stayed drained.</returns>
    public async Task<Option<SyncLease>> AcquireAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var deadline = timeProvider.GetUtcNow() + AccountLeasePolicy.WaitForRunningWork;
        while (true)
        {
            if (syncLock.TryAcquire(accountId) is SyncLockAttempt.Acquired acquired)
                return Option.Some(acquired.Lease);

            if (timeProvider.GetUtcNow() >= deadline)
                return Option.None<SyncLease>();

            await Task.Delay(AccountLeasePolicy.RetryInterval, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
