using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="leases">Takes the account's lease.</param>
/// <param name="writer">Saves the changes once the lease is held.</param>
internal sealed class LeasedSelectionRuleWriter(AccountLeaseWaiter leases, ISelectionRuleWriter writer) : ILeasedSelectionRuleWriter
{
    private const string AccountBusy = "The account is busy syncing.";

    /// <inheritdoc />
    public async Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        var lease = await leases.AcquireAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await lease.MatchAsync(held => ApplyHoldingAsync(held, accountId, changes, cancellationToken), () => Result.Failure<Unit, ErrorResponse>(new ErrorResponse(AccountBusy))).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ErrorResponse>> ApplyHoldingAsync(SyncLease lease, AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        using (lease)
        {
            return await writer.ApplyAsync(accountId, changes, cancellationToken).ConfigureAwait(false);
        }
    }
}
