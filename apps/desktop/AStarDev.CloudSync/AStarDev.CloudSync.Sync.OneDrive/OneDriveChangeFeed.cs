using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Drives.Item.Items.Item.Delta;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Reads the OneDrive change feed through Microsoft Graph delta, one page per call (#1398).</summary>
/// <param name="graphAccess">Gives the account's drive id, access token and Graph client.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports the outcome with the hashed account id, status, Graph error code and item count only.</param>
internal sealed class OneDriveChangeFeed(OneDriveGraphAccess graphAccess, TimeProvider timeProvider, ILogger<OneDriveChangeFeed> logger) : IRemoteChangeFeed
{
    private const string LocationHeader = "Location";
    private const string ApplyDifferencesCode = "resyncChangesApplyDifferences";
    private const string UploadDifferencesCode = "resyncChangesUploadDifferences";
    private const string NoCode = "none";

    /// <inheritdoc />
    public async Task<Result<RemoteChangePage, ProviderError>> GetChangesAsync(AccountId accountId, Option<ChangeCursor> cursor, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await graphAccess.DriveOfAsync(accountId, cancellationToken)
            .BindAsync(driveId => graphAccess.TokenOfAsync(accountId, outcome => OneDriveChangeLog.TokenUnavailable(logger, outcome), cancellationToken).BindAsync(token => ReadAsync(accountId, driveId, token, cursor, cancellationToken)))
            .ConfigureAwait(false);
    }

    private static Result<RemoteChangePage, ProviderError> Failed(ProviderError error) => Result.Failure<RemoteChangePage, ProviderError>(error);

    private static string RootDeltaOf(string driveId) => $"{Uri.UriSchemeHttps}://{ODataNextLinkGuard.GraphHost}/v1.0/drives/{Uri.EscapeDataString(driveId)}/root/delta";

    private static string ErrorCodeOf(ApiException exception) => exception is ODataError { Error.Code: { Length: > 0 } code } ? code : NoCode;

    private static ResyncKind ResyncKindOf(string errorCode) => errorCode switch
    {
        ApplyDifferencesCode => ResyncKindFactory.CreateApplyDifferences(),
        UploadDifferencesCode => ResyncKindFactory.CreateUploadDifferences(),
        _ => ResyncKindFactory.CreateUnknown(),
    };

    private static Option<ChangeCursor> AllowedCursor(string? link) =>
        ODataNextLinkGuard.IsAllowed(link) ? ChangeCursorFactory.Create(link).Match(cursor => Option.Some(cursor), _ => Option.None<ChangeCursor>()) : Option.None<ChangeCursor>();

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
        using var graph = graphAccess.CreateClient(accountId, accessToken);
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

        return AllowedCursor(location).Match(cursor => ExpiredFrom(cursor, errorCode), () => AllowedCursor(RootDeltaOf(driveId)).Match(cursor => ExpiredFrom(cursor, errorCode), () => Failed(ProviderErrorFactory.CreateUnexpected())));
    }

    private static Result<RemoteChangePage, ProviderError> ExpiredFrom(ChangeCursor restartFrom, string errorCode) =>
        Failed(ProviderErrorFactory.CreateCursorExpired(ResyncKindOf(errorCode), restartFrom));
}
