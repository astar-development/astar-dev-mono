using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides whether a log entry is written to the local files (#1411). An entry made for an account (it carries the hashed account id in its scope) is written below warning level only when that account is verbose. An entry for no account keeps the application's level of information and above.</summary>
public static class AccountLogLevelFilter
{
    /// <summary>Decides one entry.</summary>
    /// <param name="levels">The accounts that log verbosely.</param>
    /// <param name="level">The entry's level.</param>
    /// <param name="scope">The values of the entry's log scopes.</param>
    public static bool Admits(AccountLogLevels levels, LogLevel level, IReadOnlyDictionary<string, object?> scope) => level >= LogLevel.Warning || Admitted(levels, level, scope);

    private static bool Admitted(AccountLogLevels levels, LogLevel level, IReadOnlyDictionary<string, object?> scope) =>
        scope.TryGetValue(SyncLogScope.AccountIdHashKey, out var value) && value is string hash
            ? levels.LevelOf(hash) is AccountLogLevel.Verbose
            : level >= LogLevel.Information;
}
