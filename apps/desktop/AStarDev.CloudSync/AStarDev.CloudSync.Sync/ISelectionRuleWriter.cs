using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Saves staged changes to the selection rules of an account (#1397).</summary>
public interface ISelectionRuleWriter
{
    /// <summary>Applies the changes to the stored rules with <see cref="SelectionRulePlanner"/> and saves the result, all in one transaction. Two calls for the same account never run at the same time, so no update is lost and the rules never hold duplicates or contradictions.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="changes">The staged changes, applied in order.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns><see cref="Unit"/> once saved, or the error that stopped it; nothing is saved then.</returns>
    Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken);
}
