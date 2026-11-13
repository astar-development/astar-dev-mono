using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Downloads a OneDrive file (#1400). It reads the item's fresh pre-authenticated download URL through Graph, then streams the content with a separate request that carries no bearer token. A dropped connection resumes with a <c>Range</c> request from the target's length, up to three attempts within the call.</summary>
/// <param name="graphAccess">Gives the account's drive id, access token and Graph client.</param>
/// <param name="httpClients">Creates the client that goes through the account throttle gate and has no auth handler.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports the outcome with the hashed account id, status and byte counts only. The download URL is never logged.</param>
internal sealed class OneDriveFileDownloader(OneDriveGraphAccess graphAccess, IHttpClientFactory httpClients, TimeProvider timeProvider, ILogger<OneDriveFileDownloader> logger) : IRemoteFileDownloader
{
    private const string Location = nameof(OneDriveFileDownloader);
    private const string DownloadUrlKey = "@microsoft.graph.downloadUrl";
    private const int MaxAttempts = 3;
    private const int BufferSize = 81920;

    /// <inheritdoc />
    public async Task<Result<DownloadedFile, ProviderError>> DownloadAsync(AccountId accountId, RemoteItemId fileId, IFileInfo target, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await graphAccess.DriveOfAsync(accountId, cancellationToken)
            .BindAsync(driveId => graphAccess.TokenOfAsync(accountId, outcome => LogMessage.Warning(logger, Location, $"No access token for the download ({outcome})."), cancellationToken).BindAsync(token => AttemptAsync(accountId, driveId, token, fileId, target, progress, 1, cancellationToken)))
            .ConfigureAwait(false);
    }

    private static Result<DownloadedFile, ProviderError> Failed(ProviderError error) => Result.Failure<DownloadedFile, ProviderError>(error);

    private static Result<(DriveItem Item, string Url), ProviderError> UrlOf(DriveItem? item) =>
        item?.AdditionalData is { } data && data.TryGetValue(DownloadUrlKey, out var url) && url is string text && text.Length > 0
            ? Result.Success<(DriveItem, string), ProviderError>((item, text))
            : Result.Failure<(DriveItem, string), ProviderError>(ProviderErrorFactory.CreateNotFound());

    private static Result<DownloadedFile, ProviderError> Described(RemoteItemId fileId, DriveItem item, long size) =>
        ContentTagFactory.Create(item.CTag).Match(
            tag => Result.Success<DownloadedFile, ProviderError>(DownloadedFileFactory.Create(fileId, tag, size, ContentHashFactory.Create(item.File?.Hashes?.QuickXorHash).Match(Option.Some, _ => Option.None<ContentHash>()))),
            _ => Failed(ProviderErrorFactory.CreateUnexpected()));

    private async Task<Result<DownloadedFile, ProviderError>> AttemptAsync(AccountId accountId, string driveId, string accessToken, RemoteItemId fileId, IFileInfo target, IProgress<TransferProgress> progress, int attempt, CancellationToken cancellationToken)
    {
        var fetched = await FetchUrlAsync(accountId, driveId, accessToken, fileId, cancellationToken).ConfigureAwait(false);
        var copied = await fetched.BindAsync(found => CopyAsync(accountId, found.Url, target, progress, cancellationToken).BindAsync(size => Task.FromResult(Described(fileId, found.Item, size)))).ConfigureAwait(false);

        return await copied.MatchAsync(
            done => Task.FromResult(Result.Success<DownloadedFile, ProviderError>(done)),
            error => error is ProviderError.Transient && attempt < MaxAttempts ? AttemptAsync(accountId, driveId, accessToken, fileId, target, progress, attempt + 1, cancellationToken) : Task.FromResult(Failed(error))).ConfigureAwait(false);
    }

    private async Task<Result<(DriveItem Item, string Url), ProviderError>> FetchUrlAsync(AccountId accountId, string driveId, string accessToken, RemoteItemId fileId, CancellationToken cancellationToken)
    {
        using var graph = graphAccess.CreateClient(accountId, accessToken);
        var read = await Try.RunAsync(() => graph.Drives[driveId].Items[fileId.Value].GetAsync(cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);

        return read.Match(UrlOf, exception => Result.Failure<(DriveItem, string), ProviderError>(GraphErrorMapper.Map(exception, timeProvider).Error));
    }

    private async Task<Result<long, ProviderError>> CopyAsync(AccountId accountId, string url, IFileInfo target, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        target.Refresh();
        var start = target.Exists ? target.Length : 0;
        using var http = httpClients.CreateClient(OneDriveSyncHttp.ClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Options.Set(OneDriveSyncHttp.AccountOption, accountId);
        if (start > 0)
            request.Headers.Range = new RangeHeaderValue(start, null);

        var transferred = await Try.RunAsync(() => TransferAsync(http, request, target, start, progress, cancellationToken), cancellationToken).ConfigureAwait(false);

        return transferred.Match(result => result, exception => Result.Failure<long, ProviderError>(GraphErrorMapper.Map(exception, timeProvider).Error));
    }

    private async Task<Result<long, ProviderError>> TransferAsync(HttpClient http, HttpRequestMessage request, IFileInfo target, long start, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.RequestedRangeNotSatisfiable && start > 0)
            return Result.Success<long, ProviderError>(start);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<long, ProviderError>(StatusError(response));

        var resumed = response.StatusCode is HttpStatusCode.PartialContent;
        var offset = resumed ? start : 0;
        var total = offset + (response.Content.Headers.ContentLength ?? 0);
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = target.Open(resumed ? FileMode.OpenOrCreate : FileMode.Create, FileAccess.Write, FileShare.Read);
        _ = destination.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[BufferSize];
        var done = offset;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            done += read;
            progress.Report(TransferProgressFactory.Create(done, total));
        }

        return Result.Success<long, ProviderError>(done);
    }

    private ProviderError StatusError(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.NotFound => ProviderErrorFactory.CreateNotFound(),
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ProviderErrorFactory.CreateTransient(),
        HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable => ProviderErrorFactory.CreateThrottled(RetryAfter(response.Headers.RetryAfter)),
        >= HttpStatusCode.InternalServerError => ProviderErrorFactory.CreateTransient(),
        _ => ProviderErrorFactory.CreateUnexpected(),
    };

    private Option<TimeSpan> RetryAfter(RetryConditionHeaderValue? header) => header switch
    {
        { Delta: { } delta } => Option.Some(delta),
        { Date: { } date } => Option.Some(date - timeProvider.GetUtcNow()),
        _ => Option.None<TimeSpan>(),
    };
}
