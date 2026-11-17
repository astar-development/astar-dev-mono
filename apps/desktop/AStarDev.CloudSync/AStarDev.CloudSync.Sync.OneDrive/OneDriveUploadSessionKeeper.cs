using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using static AStarDev.CloudSync.Sync.OneDrive.OneDriveUploadSupport;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Saves, resumes, discards and deletes the session of a large upload.</summary>
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="sessions">Keeps the session of a large upload so it can continue after a restart.</param>
/// <param name="timeProvider">Tells whether a saved session has expired.</param>
/// <param name="logger">Reports counts and status codes only, never names or URLs.</param>
internal sealed class OneDriveUploadSessionKeeper(OneDriveTransferHttp http, IUploadSessionStore sessions, TimeProvider timeProvider, ILogger<OneDriveUploadSessionKeeper> logger)
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    internal async Task<Result<Unit, ProviderError>> ResumeSavedAsync(UploadTransfer transfer, CancellationToken cancellationToken)
    {
        var none = Result.Success<Unit, ProviderError>(Unit.Instance);
        if (!IsLarge(transfer))
            return none;

        var saved = await sessions.GetAsync(transfer.AccountId, transfer.Source, cancellationToken).ConfigureAwait(false);

        return await saved.Match(
            found => found.Match(session => ResumeFromAsync(transfer, session, cancellationToken), () => Task.FromResult(none)),
            _ =>
            {
                OneDriveTransferLog.SessionStoreFailed(logger);

                return Task.FromResult(none);
            }).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> ResumeFromAsync(UploadTransfer transfer, UploadSession saved, CancellationToken cancellationToken)
    {
        if (!Reusable(transfer, saved))
            return await DiscardAsync(transfer, saved, cancellationToken).ConfigureAwait(false);

        using var status = OneDriveTransferHttp.Anonymous(HttpMethod.Get, saved.Url);
        var sent = await http.SendAsync(transfer.AccountId, status, cancellationToken).ConfigureAwait(false);

        return await sent.Match(response => ReadSavedStatusAsync(transfer, saved, response, cancellationToken), error => Task.FromResult(Result.Failure<Unit, ProviderError>(error))).ConfigureAwait(false);
    }

    private bool Reusable(UploadTransfer transfer, UploadSession saved) =>
        timeProvider.GetUtcNow() + ExpiryMargin < saved.ExpiresAt && saved.SourceLength == transfer.Length && saved.SourceWriteUtc == transfer.LastWriteUtc && saved.Target == TargetOf(transfer);

    private async Task<Result<Unit, ProviderError>> ReadSavedStatusAsync(UploadTransfer transfer, UploadSession saved, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            return await ForgetGoneAsync(transfer, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var (error, statusCode, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
            OneDriveTransferLog.CallFailed(logger, statusCode, code);

            return Result.Failure<Unit, ProviderError>(error);
        }

        var next = NextOffset(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        return await next.Match(
            offset => offset < transfer.Length ? Task.FromResult(Resumed(transfer, saved, offset)) : DiscardAsync(transfer, saved, cancellationToken),
            () => DiscardAsync(transfer, saved, cancellationToken)).ConfigureAwait(false);
    }

    private Result<Unit, ProviderError> Resumed(UploadTransfer transfer, UploadSession saved, long offset)
    {
        transfer.Session = Option.Some(saved.Url);
        transfer.ExpiresAt = Option.Some(saved.ExpiresAt);
        transfer.Offset = offset;
        transfer.HashedUpTo = 0;
        _ = transfer.Hash.GetHashAndReset();
        OneDriveTransferLog.UploadResumed(logger, offset, 0);

        return Result.Success<Unit, ProviderError>(Unit.Instance);
    }

    private async Task<Result<Unit, ProviderError>> ForgetGoneAsync(UploadTransfer transfer, CancellationToken cancellationToken)
    {
        OneDriveTransferLog.SessionRestarted(logger);
        await ForgetAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ProviderError>(Unit.Instance);
    }

    private async Task<Result<Unit, ProviderError>> DiscardAsync(UploadTransfer transfer, UploadSession saved, CancellationToken cancellationToken)
    {
        OneDriveTransferLog.SessionDiscarded(logger);
        await DeleteAsync(transfer.AccountId, saved.Url, cancellationToken).ConfigureAwait(false);
        await ForgetAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ProviderError>(Unit.Instance);
    }

    internal async Task ForgetAsync(UploadTransfer transfer, CancellationToken cancellationToken)
    {
        if (!IsLarge(transfer))
            return;

        var removed = await sessions.RemoveAsync(transfer.AccountId, transfer.Source, cancellationToken).ConfigureAwait(false);
        _ = removed.Match(_ => Unit.Instance, _ => LogStoreFailed());
    }

    internal async Task SaveProgressAsync(UploadTransfer transfer, CancellationToken cancellationToken)
    {
        if (!IsLarge(transfer) || transfer.Session is not Option<Uri>.Some { Value: var url } || transfer.ExpiresAt is not Option<DateTimeOffset>.Some { Value: var expires })
            return;

        var saved = await sessions.SaveAsync(transfer.AccountId, transfer.Source, UploadSessionFactory.Create(url, expires, transfer.Length, transfer.LastWriteUtc, transfer.Offset, TargetOf(transfer)), cancellationToken).ConfigureAwait(false);
        _ = saved.Match(_ => Unit.Instance, _ => LogStoreFailed());
    }

    private Unit LogStoreFailed()
    {
        OneDriveTransferLog.SessionStoreFailed(logger);

        return Unit.Instance;
    }

    internal async Task DeleteSessionAsync(UploadTransfer transfer, CancellationToken cancellationToken)
    {
        await transfer.Session.Match(session => DeleteAsync(transfer.AccountId, session, cancellationToken), () => Task.CompletedTask).ConfigureAwait(false);
        await ForgetAsync(transfer, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteAsync(AccountId accountId, Uri session, CancellationToken cancellationToken)
    {
        using var request = OneDriveTransferHttp.Anonymous(HttpMethod.Delete, session);
        using var outcome = (await http.SendAsync(accountId, request, cancellationToken).ConfigureAwait(false)).Match(response => response, _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
