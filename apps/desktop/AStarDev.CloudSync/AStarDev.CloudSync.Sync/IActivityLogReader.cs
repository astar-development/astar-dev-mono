using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the activity log (#1564).</summary>
public interface IActivityLogReader
{
    /// <summary>Reads the newest entries, newest first.</summary>
    /// <param name="accountId">The account to read, or none for every account.</param>
    /// <param name="count">The most entries to return. Zero or less returns none.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the error that stopped the read.</returns>
    Task<Result<IReadOnlyList<ActivityEntry>, ErrorResponse>> GetRecentAsync(Option<AccountId> accountId, int count, CancellationToken cancellationToken);
}
