using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;

namespace AStarDev.CloudSync.Sync;

/// <summary>The accounts that log verbosely, held in memory so the logging can ask on every entry (#1411). It is keyed by the hashed account id that every sync log scope carries, so the id itself is never needed.</summary>
public sealed class AccountLogLevels
{
    private readonly ConcurrentDictionary<string, byte> verbose = new(StringComparer.Ordinal);

    /// <summary>Applies an account's level to the running logging.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="level">The level.</param>
    public void Apply(AccountId accountId, AccountLogLevel level)
    {
        var hash = HashOf(accountId);
        if (level is AccountLogLevel.Verbose)
            verbose[hash] = 0;
        else
            _ = verbose.TryRemove(hash, out _);
    }

    /// <summary>Tells the level of the account with this hashed id.</summary>
    /// <param name="accountIdHash">The value of the <see cref="SyncLogScope.AccountIdHashKey"/> scope.</param>
    public AccountLogLevel LevelOf(string accountIdHash) => verbose.ContainsKey(accountIdHash) ? AccountLogLevelFactory.CreateVerbose() : AccountLogLevelFactory.CreateStandard();

    /// <summary>Starts with the accounts the settings say are verbose. Until it finishes every account counts as standard.</summary>
    /// <param name="settings">Lists the verbose accounts.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public Task<Result<Unit, ErrorResponse>> LoadAsync(IAccountLogLevelSettings settings, CancellationToken cancellationToken) =>
        settings.ListVerboseAsync(cancellationToken).MatchAsync(
            accounts => Result.Success<Unit, ErrorResponse>(ApplyAll(accounts)),
            Result.Failure<Unit, ErrorResponse>);

    private static string HashOf(AccountId accountId) => LogHash.Of(accountId.Value.ToString());

    private Unit ApplyAll(IReadOnlyList<AccountId> accounts)
    {
        foreach (var account in accounts)
            Apply(account, AccountLogLevelFactory.CreateVerbose());

        return Unit.Instance;
    }
}
