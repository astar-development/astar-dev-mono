using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Delta;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
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
    private const string DriveUrlPrefix = "https://graph.microsoft.com/v1.0/drives/";
    private const string RootDeltaSuffix = "/root/delta";
    private const string LocationHeader = "Location";
    private const string ApplyDifferencesCode = "resyncChangesApplyDifferences";
    private const string UploadDifferencesCode = "resyncChangesUploadDifferences";
    private const string NoCode = "none";

    /// <inheritdoc />
    public async Task<Result<RemoteChangePage, ProviderError>> GetChangesAsync(AccountId accountId, Option<ChangeCursor> cursor, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await DriveOfAsync(accountId, cancellationToken)
            .BindAsync(driveId => TokenOfAsync(accountId, cancellationToken).BindAsync(token => ReadAsync(accountId, driveId, token, cursor, cancellationToken)))
            .ConfigureAwait(false);
    }

    private static Result<RemoteChangePage, ProviderError> Failed(ProviderError error) => Result.Failure<RemoteChangePage, ProviderError>(error);

    private static string RootDeltaOf(string driveId) => DriveUrlPrefix + Uri.EscapeDataString(driveId) + RootDeltaSuffix;

    private static string ErrorCodeOf(ApiException exception) => exception is ODataError { Error.Code: { Length: > 0 } code } ? code : NoCode;

    private static ResyncKind ResyncKindOf(string errorCode) => errorCode switch
    {
        ApplyDifferencesCode => ResyncKindFactory.CreateApplyDifferences(),
        UploadDifferencesCode => ResyncKindFactory.CreateUploadDifferences(),
        _ => ResyncKindFactory.CreateUnknown(),
    };

    private static Option<ChangeCursor> AllowedCursor(string? link) =>
        ODataNextLinkGuard.IsAllowed(link) ? ChangeCursorFactory.Create(link).Match(cursor => Option.Some(cursor), _ => Option.None<ChangeCursor>()) : Option.None<ChangeCursor>();

    private async Task<Result<string, ProviderError>> DriveOfAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await accounts.GetAllAsync(cancellationToken)
            .MatchAsync(
                stored => stored.FirstOrNone(account => account.Id == accountId).Match(account => Result.Success<string, ProviderError>(account.Registration.DriveId.Value), () => Result.Failure<string, ProviderError>(ProviderErrorFactory.CreateNotFound())),
                _ => Result.Failure<string, ProviderError>(ProviderErrorFactory.CreateTransient()))
            .ConfigureAwait(false);

    private async Task<Result<string, ProviderError>> TokenOfAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var outcome = await tokens.GetAccessTokenAsync(accountId, cancellationToken).ConfigureAwait(false);

        return outcome switch
        {
            AccessTokenOutcome.Acquired acquired => Result.Success<string, ProviderError>(acquired.Token),
            AccessTokenOutcome.SignInRequired => Refused(outcome, ProviderErrorFactory.CreateAuthenticationRequired()),
            AccessTokenOutcome.UnknownAccount => Refused(outcome, ProviderErrorFactory.CreateNotFound()),
            _ => Refused(outcome, ProviderErrorFactory.CreateTransient()),
        };
    }

    private Result<string, ProviderError> Refused(AccessTokenOutcome outcome, ProviderError error)
    {
        OneDriveChangeLog.TokenUnavailable(logger, outcome.GetType().Name);

        return Result.Failure<string, ProviderError>(error);
    }

    private GraphServiceClient CreateClient(AccountId accountId, string accessToken)
    {
        var final = new AccountStampHandler(accountId, handlers.CreateHandler(OneDriveSyncHttp.ClientName));
        var pipeline = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(final, [.. KiotaClientFactory.CreateDefaultHandlers([retry])]);

        return new GraphServiceClient(new HttpClient(pipeline!, disposeHandler: true), new BaseBearerTokenAuthenticationProvider(new BearerTokenProvider(accessToken)));
    }

    private async Task<Result<RemoteChangePage, ProviderError>> ReadAsync(AccountId accountId, string driveId, string accessToken, Option<ChangeCursor> cursor, CancellationToken cancellationToken)
    {
        var link = cursor.Match(stored => stored.Value, () => RootDeltaOf(driveId));

        if (!ODataNextLinkGuard.IsAllowed(link))
        {
            OneDriveChangeLog.LinkRefused(logger);

            return Failed(ProviderErrorFactory.CreateUnexpected());
        }

        var reading = ReadPageAsync(accountId, accessToken, link, cancellationToken);
        await ((Task)reading).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (cancellationToken.IsCancellationRequested)
        {
            OneDriveChangeLog.ReadCancelled(logger);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (reading.IsCanceled)
            return Faulted(new TaskCanceledException(), driveId);

        return reading.Exception is { InnerException: { } exception } ? Faulted(exception, driveId) : reading.Result;
    }

    private async Task<Result<RemoteChangePage, ProviderError>> ReadPageAsync(AccountId accountId, string accessToken, string link, CancellationToken cancellationToken)
    {
        using var graph = CreateClient(accountId, accessToken);
        var response = await new DeltaRequestBuilder(link, graph.RequestAdapter).GetAsDeltaGetResponseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var items = DeltaItemMapper.Map(response?.Value).ToList();

        return ContinuationOf(response?.OdataNextLink, response?.OdataDeltaLink).Match(
            continuation =>
            {
                OneDriveChangeLog.PageRead(logger, items.Count, continuation is PageContinuation.CaughtUp);

                return Result.Success<RemoteChangePage, ProviderError>(RemoteChangePageFactory.Create(items, continuation));
            },
            () =>
            {
                OneDriveChangeLog.LinkRefused(logger);

                return Failed(ProviderErrorFactory.CreateUnexpected());
            });
    }

    private static Option<PageContinuation> ContinuationOf(string? nextLink, string? deltaLink) =>
        string.IsNullOrEmpty(nextLink)
            ? AllowedCursor(deltaLink).Map(PageContinuationFactory.CreateCaughtUp)
            : AllowedCursor(nextLink).Map(PageContinuationFactory.CreateMorePages);

    private Result<RemoteChangePage, ProviderError> Faulted(Exception exception, string driveId)
    {
        if (exception is AggregateException { InnerExceptions.Count: > 0 } aggregate)
            return Faulted(aggregate.InnerExceptions[^1], driveId);

        if (exception is ApiException { ResponseStatusCode: (int)System.Net.HttpStatusCode.Gone } gone)
            return Expired(gone, driveId);

        var (error, statusCode, errorCode) = GraphErrorMapper.Map(exception, timeProvider);
        OneDriveChangeLog.ReadFailed(logger, statusCode, errorCode, exception.GetType().Name);

        return Failed(error);
    }

    private Result<RemoteChangePage, ProviderError> Expired(ApiException gone, string driveId)
    {
        var errorCode = ErrorCodeOf(gone);
        OneDriveChangeLog.CursorExpired(logger, errorCode);
        var location = gone.ResponseHeaders.TryGetValue(LocationHeader, out var values) ? values.FirstOrDefault() : null;
        var restartFrom = AllowedCursor(location).Match(cursor => cursor, () => ChangeCursorFactory.Create(RootDeltaOf(driveId)).Match(cursor => cursor, _ => default));

        return Failed(ProviderErrorFactory.CreateCursorExpired(ResyncKindOf(errorCode), restartFrom));
    }
}
