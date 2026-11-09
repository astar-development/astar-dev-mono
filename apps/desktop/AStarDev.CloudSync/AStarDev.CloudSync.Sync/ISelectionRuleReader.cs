using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the stored selection rules of an account (#1397). The rules are the single source of truth for what syncs.</summary>
public interface ISelectionRuleReader
{
    /// <summary>Reads every rule of the account, sorted by path ignoring case.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rules, or the error that stopped the read. A failure is never an empty list.</returns>
    Task<Result<IReadOnlyList<SelectionRule>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken);
}
