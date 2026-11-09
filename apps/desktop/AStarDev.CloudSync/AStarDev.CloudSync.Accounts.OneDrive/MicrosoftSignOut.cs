using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Removes a stored account's MSAL tokens. The token source keeps no per-account cache, so nothing else needs evicting.</summary>
/// <param name="tokenCache">Connects the token cache (never asking), so removal reaches the persisted tokens.</param>
/// <param name="accountStore">Maps the account id to its key.</param>
/// <param name="msal">Removes the account from the token cache.</param>
/// <param name="logger">Reports a failed removal by exception type only.</param>
internal sealed class MicrosoftSignOut(TokenCacheInitialiser tokenCache, IAccountStore accountStore, IMsalClient msal, ILogger<MicrosoftSignOut> logger) : IProviderSignOut
{
    private const string Location = nameof(MicrosoftSignOut);

    /// <inheritdoc />
    public Task<Result<Unit, ProviderError>> SignOutAsync(AccountId accountId, CancellationToken cancellationToken) =>
        accountStore.GetAllAsync(cancellationToken)
            .MatchAsync(
                accounts => accounts.FirstOrNone(account => account.Id == accountId).MatchAsync(account => ConnectThenRemoveTokensAsync(account.Registration.Key, cancellationToken), () => Task.FromResult(Result.Success<Unit, ProviderError>(Unit.Instance))),
                _ => Task.FromResult(Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateTransient())));

    private Task<Result<Unit, ProviderError>> ConnectThenRemoveTokensAsync(ProviderAccountKey key, CancellationToken cancellationToken) =>
        tokenCache.EnsureAsync(ConsentPromptFactory.CreateNever(), cancellationToken)
            .MatchAsync(_ => RemoveTokensAsync(key), _ => Task.FromResult(CacheUnavailable()));

    private Result<Unit, ProviderError> CacheUnavailable()
    {
        LogMessage.Warning(logger, Location, "The token cache could not be connected, so the account's tokens were not removed.");

        return Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateTransient());
    }

    private Task<Result<Unit, ProviderError>> RemoveTokensAsync(ProviderAccountKey key) =>
        Try.RunAsync(() => UnitTask.RunAsync(() => msal.RemoveAccountAsync(key)), CancellationToken.None)
            .MatchAsync(Result.Success<Unit, ProviderError>, Failed);

    private Result<Unit, ProviderError> Failed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateUnexpected());
    }
}
