using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Gets tokens from the MSAL token cache for accounts saved in the account store.</summary>
/// <param name="tokenCache">Connects the token cache before it is read.</param>
/// <param name="accountStore">Maps the account id to the provider key MSAL knows.</param>
/// <param name="msal">Reads the token silently.</param>
/// <param name="logger">Reports tokens that cannot be obtained.</param>
internal sealed class MsalAccessTokenSource(TokenCacheInitialiser tokenCache, IAccountStore accountStore, IMsalClient msal, ILogger<MsalAccessTokenSource> logger) : IAccessTokenSource
{
    private const string Location = nameof(MsalAccessTokenSource);
    private const string TimeoutErrorType = "Timeout";

    /// <inheritdoc />
    public Task<AccessTokenOutcome> GetAccessTokenAsync(AccountId accountId, CancellationToken cancellationToken) =>
        tokenCache.EnsureAsync(ConsentPromptFactory.CreateNever(), cancellationToken)
            .MatchAsync(_ => FindAndAcquireAsync(accountId, cancellationToken), _ => Task.FromResult(TokenCacheUnavailable()));

    private Task<AccessTokenOutcome> FindAndAcquireAsync(AccountId accountId, CancellationToken cancellationToken) =>
        accountStore.GetAllAsync(cancellationToken)
            .MatchAsync(
                accounts => accounts.FirstOrNone(account => account.Id == accountId).MatchAsync(account => AcquireAsync(account.Registration.Key, cancellationToken), AccessTokenOutcomeFactory.CreateUnknownAccount),
                _ => Task.FromResult(AccessTokenOutcomeFactory.CreateUnavailable()));

    private async Task<AccessTokenOutcome> AcquireAsync(ProviderAccountKey key, CancellationToken cancellationToken)
    {
        var request = RequestAsync(key, cancellationToken);
        await ((Task)request).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        cancellationToken.ThrowIfCancellationRequested();

        if (request.IsCanceled)
            return Unavailable("The token request timed out.", TimeoutErrorType);

        return request.Exception is { InnerException: { } exception } ? ToOutcome(exception) : request.Result.Match(AccessTokenOutcomeFactory.CreateAcquired, AccessTokenOutcomeFactory.CreateAccountNotCached);
    }

    private async Task<Option<string>> RequestAsync(ProviderAccountKey key, CancellationToken cancellationToken) => await msal.AcquireTokenSilentAsync(key, cancellationToken).ConfigureAwait(false);

    private AccessTokenOutcome ToOutcome(Exception exception)
    {
        if (exception is MsalUiRequiredException)
        {
            LogMessage.Warning(logger, Location, "The account must sign in again.");

            return AccessTokenOutcomeFactory.CreateSignInRequired();
        }

        return Unavailable("Could not get an access token.", exception.ErrorType());
    }

    private AccessTokenOutcome TokenCacheUnavailable()
    {
        LogMessage.Warning(logger, Location, "The token cache cannot be connected without asking the user.");

        return AccessTokenOutcomeFactory.CreateTokenCacheUnavailable();
    }

    private AccessTokenOutcome Unavailable(string message, string errorType)
    {
        LogMessage.Error(logger, message, errorType);

        return AccessTokenOutcomeFactory.CreateUnavailable();
    }
}
