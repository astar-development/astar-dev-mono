using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the attention queue (#1405).</summary>
public interface ISyncAttentionReader
{
    /// <summary>Reads every open item of the account, oldest first.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The items, or the error that stopped the read. A failure is never an empty list.</returns>
    Task<Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse>> GetOpenAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Reads the paths of the account's open conflicts.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The paths, or the error that stopped the read. A failure is never an empty list.</returns>
    Task<Result<IReadOnlyList<RelativeSyncPath>, ErrorResponse>> GetConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Counts the account's open items.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The counts, or the error that stopped the read.</returns>
    Task<Result<AttentionCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken);
}
