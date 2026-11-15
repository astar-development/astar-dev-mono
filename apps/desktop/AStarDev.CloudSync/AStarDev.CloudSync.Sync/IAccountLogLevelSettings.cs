using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads and changes how much each account writes to the local log files (#1411). A change applies straight away, with no restart.</summary>
public interface IAccountLogLevelSettings
{
    /// <summary>Reads the account's level.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The level, which is <see cref="AccountLogLevel.Standard"/> for an account that never changed it, or why it could not be read.</returns>
    Task<Result<AccountLogLevel, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Saves the account's level and applies it to the running logging.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="level">The new level.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, AccountLogLevel level, CancellationToken cancellationToken);

    /// <summary>Lists the accounts that log verbosely, so the logging can start with them.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> ListVerboseAsync(CancellationToken cancellationToken);
}
