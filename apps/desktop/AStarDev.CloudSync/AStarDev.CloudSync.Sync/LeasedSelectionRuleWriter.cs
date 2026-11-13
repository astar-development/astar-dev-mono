using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="leases">Takes the account's lease.</param>
/// <param name="writer">Saves the changes once the lease is held.</param>
internal sealed class LeasedSelectionRuleWriter(AccountLeaseWaiter leases, ISelectionRuleWriter writer) : ILeasedSelectionRuleWriter
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{leases}{writer}{accountId}{changes}{cancellationToken}");
}
