using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Moves selection rules to follow folders that were renamed or moved remotely (#1398).</summary>
public interface ISelectionRuleRelocator
{
    /// <summary>Recomputes the path of every rule that has a remote id and saves the rules whose folder moved. A rule whose new path already has a rule is dropped: the existing rule wins.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="moves">The moves just applied to the index; with none, nothing is done.</param>
    /// <param name="cancellationToken">Cancels the relocation.</param>
    /// <returns>How many rules followed and how many were dropped, or the error that stopped it.</returns>
    Task<Result<RuleRelocationSummary, ErrorResponse>> RelocateAsync(AccountId accountId, IReadOnlyList<RemoteChange.Moved> moves, CancellationToken cancellationToken);
}
