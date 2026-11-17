using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using static AStarDev.CloudSync.Sync.OneDrive.OneDriveUploadSupport;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Sends a large file to its upload session fragment by fragment, resuming and restarting on failure.</summary>
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="keeper">Saves, forgets and deletes the session as the upload advances.</param>
/// <param name="creator">Creates a new session when Graph reports the old one gone.</param>
/// <param name="logger">Reports counts and status codes only, never names or URLs.</param>
internal sealed class OneDriveFragmentUploader(OneDriveTransferHttp http, OneDriveUploadSessionKeeper keeper, OneDriveUploadSessionCreator creator, ILogger<OneDriveFragmentUploader> logger)
{
    private const int FragmentBytes = 10 * 1024 * 1024;
    private const int MaxAttempts = 5;
    private const int MaxRestarts = 2;

    private static string HexOf(IncrementalHash hash) => Convert.ToHexStringLower(hash.GetHashAndReset());

    private static bool Changed(IFileInfo source, long length, DateTime lastWriteUtc)
    {
        source.Refresh();

        return !source.Exists || source.Length != length || source.LastWriteTimeUtc != lastWriteUtc;
    }

    internal async Task<Result<UploadedFile, ProviderError>> SendFragmentsAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        Option<Result<UploadedFile, ProviderError>> outcome;
        do
        {
            var length = Math.Min(FragmentBytes, transfer.Length - transfer.Offset);
            if (transfer.Offset + length == transfer.Length && Changed(transfer.Source, transfer.Length, transfer.LastWriteUtc))
                return await AbandonAsync(transfer, OneDriveTransferLog.SourceChanged, cancellationToken).ConfigureAwait(false);

            var read = await ReadFragmentAsync(transfer, length, cancellationToken).ConfigureAwait(false);
            if (read.Match(_ => false, _ => true))
                return await AbandonAsync(transfer, OneDriveTransferLog.SourceChanged, cancellationToken).ConfigureAwait(false);

            outcome = await read.Match(buffer => PutFragmentAsync(transfer, credentials, buffer, cancellationToken), error => Task.FromResult(Option.Some(Result.Failure<UploadedFile, ProviderError>(error)))).ConfigureAwait(false);
        }
        while (outcome.Match(_ => false, () => true));

        return outcome.Match(done => done, () => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
    }

    private static async Task<Result<byte[], ProviderError>> ReadFragmentAsync(UploadTransfer transfer, long length, CancellationToken cancellationToken) =>
        await Try.RunAsync(() => ReadBytesAsync(transfer.Source, transfer.Offset, length, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<byte[], ProviderError>, _ => Result.Failure<byte[], ProviderError>(ProviderErrorFactory.CreateSourceChanged())).ConfigureAwait(false);

    private static async Task<byte[]> ReadBytesAsync(IFileInfo source, long offset, long length, CancellationToken cancellationToken)
    {
        await using var stream = source.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _ = stream.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[length];
        await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);

        return buffer;
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> PutFragmentAsync(UploadTransfer transfer, TransferCredentials credentials, byte[] buffer, CancellationToken cancellationToken)
    {
        var session = transfer.Session.Match(found => found, () => throw new InvalidOperationException("The session is created before fragments are sent."));
        var start = transfer.Offset;
        if (start <= transfer.HashedUpTo && start + buffer.Length > transfer.HashedUpTo)
        {
            transfer.Hash.AppendData(buffer.AsSpan((int)(transfer.HashedUpTo - start)));
            transfer.HashedUpTo = start + buffer.Length;
        }

        using var request = OneDriveTransferHttp.Anonymous(HttpMethod.Put, session);
        request.Content = new ByteArrayContent(buffer);
        request.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, start + buffer.Length - 1, transfer.Length);
        var sent = await http.SendAsync(transfer.AccountId, request, cancellationToken).ConfigureAwait(false);

        return await sent.Match(response => ReadFragmentResponseAsync(transfer, credentials, response, cancellationToken), error => ResumeAsync(transfer, error, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> ReadFragmentResponseAsync(UploadTransfer transfer, TransferCredentials credentials, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
            return Option.Some(await CompleteAsync(transfer, response, cancellationToken).ConfigureAwait(false));

        if (response.StatusCode is HttpStatusCode.Accepted)
            return await AdvanceAsync(transfer, response, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotFound)
            return await RestartAsync(transfer, credentials, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.RequestedRangeNotSatisfiable || ((int)response.StatusCode >= 500 && response.StatusCode is not HttpStatusCode.InsufficientStorage))
            return await ResumeAsync(transfer, ProviderErrorFactory.CreateTransient(), cancellationToken).ConfigureAwait(false);

        var (error, status, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.CallFailed(logger, status, code);

        return Option.Some(await AbandonAsync(transfer, error, cancellationToken).ConfigureAwait(false));
    }

    private async Task<Result<UploadedFile, ProviderError>> CompleteAsync(UploadTransfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await keeper.ForgetAsync(transfer, cancellationToken).ConfigureAwait(false);
        var hash = transfer.HashedUpTo == transfer.Length ? HexOf(transfer.Hash) : await HashAgainAsync(transfer, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.UploadCompleted(logger, transfer.Length, transfer.Fragments + 1);

        return ToUploaded(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), transfer.Length, hash);
    }

    private static async Task<string> HashAgainAsync(UploadTransfer transfer, CancellationToken cancellationToken)
    {
        await using var stream = transfer.Source.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> AdvanceAsync(UploadTransfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var next = NextOffset(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        transfer.Fragments++;
        var advanced = next.Match(
            offset => Advanced(transfer, offset),
            () => Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())));
        await keeper.SaveProgressAsync(transfer, cancellationToken).ConfigureAwait(false);

        return advanced;
    }

    private static Option<Result<UploadedFile, ProviderError>> Advanced(UploadTransfer transfer, long offset)
    {
        transfer.Attempts = offset > transfer.Offset ? 0 : transfer.Attempts + 1;
        transfer.Offset = offset;
        transfer.Progress.Report(TransferProgressFactory.Create(offset, transfer.Length));

        return transfer.Attempts > MaxAttempts
            ? Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient()))
            : Option.None<Result<UploadedFile, ProviderError>>();
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> ResumeAsync(UploadTransfer transfer, ProviderError cause, CancellationToken cancellationToken)
    {
        transfer.Attempts++;
        if (transfer.Attempts > MaxAttempts)
            return Option.Some(await GiveUpAsync(transfer, cause, cancellationToken).ConfigureAwait(false));

        var session = transfer.Session.Match(found => found, () => throw new InvalidOperationException("The session is created before fragments are sent."));
        using var status = OneDriveTransferHttp.Anonymous(HttpMethod.Get, session);
        var sent = await http.SendAsync(transfer.AccountId, status, cancellationToken).ConfigureAwait(false);

        return await sent.Match(response => ReadStatusAsync(transfer, response, cancellationToken), error => Task.FromResult(Option.Some(Result.Failure<UploadedFile, ProviderError>(error)))).ConfigureAwait(false);
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> ReadStatusAsync(UploadTransfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (!response.IsSuccessStatusCode)
        {
            var (error, statusCode, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
            OneDriveTransferLog.CallFailed(logger, statusCode, code);

            return Option.Some(Result.Failure<UploadedFile, ProviderError>(error));
        }

        return NextOffset(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Match(
            offset =>
            {
                transfer.Offset = offset;
                OneDriveTransferLog.UploadResumed(logger, offset, transfer.Attempts);

                return Option.None<Result<UploadedFile, ProviderError>>();
            },
            () => Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())));
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> RestartAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        transfer.Restarts++;
        if (transfer.Restarts > MaxRestarts)
            return Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient()));

        OneDriveTransferLog.SessionRestarted(logger);
        var created = await creator.CreateSessionAsync(transfer, credentials, cancellationToken).ConfigureAwait(false);

        return created.Match(_ => Option.None<Result<UploadedFile, ProviderError>>(), error => Option.Some(Result.Failure<UploadedFile, ProviderError>(error)));
    }

    private async Task<Result<UploadedFile, ProviderError>> AbandonAsync(UploadTransfer transfer, Action<ILogger> log, CancellationToken cancellationToken)
    {
        log(logger);
        await keeper.DeleteSessionAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateSourceChanged());
    }

    private async Task<Result<UploadedFile, ProviderError>> AbandonAsync(UploadTransfer transfer, ProviderError error, CancellationToken cancellationToken)
    {
        await keeper.DeleteSessionAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Failure<UploadedFile, ProviderError>(error);
    }

    private async Task<Result<UploadedFile, ProviderError>> GiveUpAsync(UploadTransfer transfer, ProviderError error, CancellationToken cancellationToken) =>
        IsLarge(transfer) ? Result.Failure<UploadedFile, ProviderError>(error) : await AbandonAsync(transfer, error, cancellationToken).ConfigureAwait(false);
}
