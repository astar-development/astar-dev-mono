using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Settles open conflicts of one account with one strategy (#1406). It holds the account's sync lease for the whole call, so a sync pass never runs at the same time.</summary>
public interface IConflictResolver
{
    /// <summary>Resolves the conflicts. A strategy applied to a path also settles every other open conflict at that path (CR-08). A conflict is closed only after its transfer succeeded; a failed one stays open with its reason. Skip changes nothing.</summary>
    /// <param name="accountId">The account the conflicts belong to.</param>
    /// <param name="ids">The conflicts to resolve.</param>
    /// <param name="strategy">The strategy to apply.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>What was resolved, failed and skipped, or an error when the call was refused because a sync pass is running or the queue could not be read.</returns>
    Task<Result<ResolutionSummary, ErrorResponse>> ResolveAsync(AccountId accountId, IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, CancellationToken cancellationToken);
}
