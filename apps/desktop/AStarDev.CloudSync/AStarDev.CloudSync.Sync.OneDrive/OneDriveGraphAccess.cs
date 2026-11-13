using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Http;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Gives the OneDrive readers an account's drive id, its access token and a Graph client that runs through the shared HTTP pipeline.</summary>
/// <param name="tokens">Gives the account's access token without asking the user.</param>
/// <param name="accounts">Holds the account's drive id.</param>
/// <param name="handlers">Creates the shared HTTP pipeline, which includes the account throttle gate.</param>
/// <param name="retry">The Graph SDK retry settings for 429 and 503.</param>
internal sealed class OneDriveGraphAccess(IAccessTokenSource tokens, IAccountStore accounts, IHttpMessageHandlerFactory handlers, RetryHandlerOption retry)
{
    internal async Task<Result<string, ProviderError>> DriveOfAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await accounts.GetAllAsync(cancellationToken)
            .MatchAsync(
                stored => stored.FirstOrNone(account => account.Id == accountId).Match(account => Result.Success<string, ProviderError>(account.Registration.DriveId.Value), () => Result.Failure<string, ProviderError>(ProviderErrorFactory.CreateNotFound())),
                _ => Result.Failure<string, ProviderError>(ProviderErrorFactory.CreateTransient()))
            .ConfigureAwait(false);

    internal async Task<Result<string, ProviderError>> TokenOfAsync(AccountId accountId, Action<string> onRefused, CancellationToken cancellationToken)
    {
        var outcome = await tokens.GetAccessTokenAsync(accountId, cancellationToken).ConfigureAwait(false);

        return outcome switch
        {
            AccessTokenOutcome.Acquired acquired => Result.Success<string, ProviderError>(acquired.Token),
            AccessTokenOutcome.SignInRequired => Refused(outcome, onRefused, ProviderErrorFactory.CreateAuthenticationRequired()),
            AccessTokenOutcome.UnknownAccount => Refused(outcome, onRefused, ProviderErrorFactory.CreateNotFound()),
            _ => Refused(outcome, onRefused, ProviderErrorFactory.CreateTransient()),
        };
    }

    internal GraphServiceClient CreateClient(AccountId accountId, string accessToken)
    {
        var final = new AccountStampHandler(accountId, handlers.CreateHandler(OneDriveSyncHttp.ClientName));
        var pipeline = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(final, [.. KiotaClientFactory.CreateDefaultHandlers([retry])]);

        return new GraphServiceClient(new HttpClient(pipeline!, disposeHandler: true), new BaseBearerTokenAuthenticationProvider(new BearerTokenProvider(accessToken)));
    }

    private static Result<string, ProviderError> Refused(AccessTokenOutcome outcome, Action<string> onRefused, ProviderError error)
    {
        onRefused(outcome.GetType().Name);

        return Result.Failure<string, ProviderError>(error);
    }
}
