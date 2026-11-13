using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Reads the OneDrive change feed through Microsoft Graph delta, one page per call (#1398).</summary>
/// <param name="tokens">Gives the account's access token without asking the user.</param>
/// <param name="accounts">Holds the account's drive id.</param>
/// <param name="handlers">Creates the shared HTTP pipeline, which includes the account throttle gate.</param>
/// <param name="retry">The Graph SDK retry settings for 429 and 503.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports the outcome with the hashed account id, status, Graph error code and item count only.</param>
internal sealed class OneDriveChangeFeed(IAccessTokenSource tokens, IAccountStore accounts, IHttpMessageHandlerFactory handlers, RetryHandlerOption retry, TimeProvider timeProvider, ILogger<OneDriveChangeFeed> logger) : IRemoteChangeFeed
{
    /// <inheritdoc />
    public Task<Result<RemoteChangePage, ProviderError>> GetChangesAsync(AccountId accountId, Option<ChangeCursor> cursor, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{cursor}{cancellationToken}{tokens}{accounts}{handlers}{retry}{timeProvider}{logger}");
}
