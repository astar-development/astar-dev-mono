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
    /// <returns>The lease, or none when the account stayed busy for the whole wait or the lock is drained (no wait: an update or shutdown is under way).</returns>
    public async Task<Option<SyncLease>> AcquireAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var deadline = timeProvider.GetUtcNow() + AccountLeasePolicy.WaitForRunningWork;
        while (true)
        {
            var attempt = await AttemptAsync(accountId, deadline - timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
            if (attempt is SyncLockAttempt.Acquired acquired)
                return Option.Some(acquired.Lease);

            if (attempt is SyncLockAttempt.Draining || timeProvider.GetUtcNow() >= deadline)
                return Option.None<SyncLease>();

            await Task.Delay(AccountLeasePolicy.RetryInterval, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<SyncLockAttempt> AttemptAsync(AccountId accountId, TimeSpan remaining, CancellationToken cancellationToken)
    {
        using var budget = new CancellationTokenSource(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budget.Token);

        return await syncLock.AcquireAsync(accountId, linked.Token).ConfigureAwait(false);
    }
}
