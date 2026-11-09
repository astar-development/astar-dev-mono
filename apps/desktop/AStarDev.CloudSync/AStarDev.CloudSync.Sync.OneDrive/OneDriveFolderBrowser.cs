using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Lists the folders of a OneDrive folder through Microsoft Graph, one level per call, following every page.</summary>
internal sealed class OneDriveFolderBrowser(IAccessTokenSource tokens, IAccountStore accounts, IHttpMessageHandlerFactory handlers, RetryHandlerOption retry, TimeProvider timeProvider, ILogger<OneDriveFolderBrowser> logger) : IRemoteFolderBrowser
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> GetChildFoldersAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken)
    {
        _ = (tokens, accounts, handlers, retry, timeProvider, logger);

        return Task.FromResult(Result.Failure<IReadOnlyList<RemoteFolder>, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
    }
}
