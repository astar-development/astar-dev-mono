using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Saves selection changes made in the UI while holding the account's sync lease, so a save cannot race a running pass or an account removal. Code that already runs inside a sync pass uses <see cref="ISelectionRuleWriter"/> instead.</summary>
public interface ILeasedSelectionRuleWriter
{
    /// <summary>Takes the account's lease, saves the changes, then releases the lease.</summary>
    /// <param name="accountId">The account whose rules change.</param>
    /// <param name="changes">The changes to save.</param>
    /// <param name="cancellationToken">Cancels the wait for the lease and the save.</param>
    /// <returns>Success, or the failure when the account stayed busy for the whole wait or the save failed.</returns>
    Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken);
}
