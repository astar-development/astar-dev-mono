using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Assigns the default root while holding the account's sync lease, so the save cannot race an account removal.</summary>
/// <param name="leases">Takes the account's lease.</param>
/// <param name="assigner">Assigns the root once the lease is held.</param>
internal sealed class LeasedDefaultSyncRootAssigner(AccountLeaseWaiter leases, DefaultSyncRootAssigner assigner) : IDefaultSyncRootAssigner
{
    /// <inheritdoc />
    public async Task<Result<SyncRoot, SyncRootRejection>> AssignAsync(AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken)
    {
        var lease = await leases.AcquireAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await lease.MatchAsync(held => AssignHoldingAsync(held, accountId, registration, cancellationToken), () => Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateSyncStillRunning())).ConfigureAwait(false);
    }

    private async Task<Result<SyncRoot, SyncRootRejection>> AssignHoldingAsync(SyncLease lease, AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken)
    {
        using (lease)
        {
            return await assigner.AssignAsync(accountId, registration, cancellationToken).ConfigureAwait(false);
        }
    }
}
