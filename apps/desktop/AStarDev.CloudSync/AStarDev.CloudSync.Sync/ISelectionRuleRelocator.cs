using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Moves selection rules to follow folders that were renamed or moved remotely (#1398).</summary>
public interface ISelectionRuleRelocator
{
    /// <summary>Recomputes the path of every rule that has a remote id and saves the rules whose folder moved, all together in one change. A rule whose new path is held by a rule that stays, or is taken by another moved rule, is dropped: the existing rule wins. Running it again changes nothing, so it also repairs rules after an update that stopped between saving a page and relocating.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the relocation.</param>
    /// <returns>How many rules followed and how many were dropped, or the error that stopped it.</returns>
    Task<Result<RuleRelocationSummary, ErrorResponse>> RelocateAsync(AccountId accountId, CancellationToken cancellationToken);
}
