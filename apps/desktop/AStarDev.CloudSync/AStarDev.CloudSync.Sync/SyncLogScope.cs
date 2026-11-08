using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Begins the log scope every sync engine log line carries, so logs can be filtered per account (#1411) without the account id itself.</summary>
public static class SyncLogScope
{
    /// <summary>The scope key; the value is <c>LogHash.Of</c> the account id.</summary>
    public const string AccountIdHashKey = "AccountIdHash";

    /// <summary>Begins the scope for the account.</summary>
    /// <param name="logger">The engine's logger.</param>
    /// <param name="accountId">The account the work is for.</param>
    /// <returns>The scope to dispose when the work ends.</returns>
    public static IDisposable Begin(ILogger logger, AccountId accountId) => throw new NotImplementedException($"{logger}{accountId}");
}
