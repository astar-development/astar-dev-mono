using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts an account's sync baseline with a count query, so the overview never loads the baseline itself (#1565).</summary>
public interface ISyncBaselineCounter
{
    /// <summary>Counts the account's baseline folders and files.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the count.</param>
    /// <returns>The counts, all zero for an account with no baseline, or the error that stopped the count. A failure is never a zero count.</returns>
    Task<Result<BaselineCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken);
}
