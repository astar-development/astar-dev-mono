using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="logger">Reports counts and status codes only, never names or URLs.</param>
internal sealed class OneDriveFileUploader(OneDriveTransferHttp http, ILogger<OneDriveFileUploader> logger) : IRemoteFileUploader
{
    private const int FragmentBytes = 10 * 1024 * 1024;
    private const int MaxAttempts = 5;
    private const int MaxRestarts = 2;

    /// <inheritdoc />
    public async Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        var transfer = new Transfer(accountId, target, source, precondition, progress);
        var running = http.CredentialsAsync(accountId, outcome => OneDriveTransferLog.TokenUnavailable(logger, outcome), cancellationToken)
            .BindAsync(credentials => RunAsync(transfer, credentials, cancellationToken));
        await ((Task)running).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (running.IsCanceled)
        {
            await CancelSessionAsync(transfer).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return await running.ConfigureAwait(false);
    }

    private static string EmptyHash() => Convert.ToHexStringLower(SHA256.HashData([]));

    private static string HexOf(IncrementalHash hash) => Convert.ToHexStringLower(hash.GetHashAndReset());

    private static Unit Matched(HttpRequestMessage request, string tag)
    {
        OneDriveTransferHttp.AddIfMatch(request, tag);

        return Unit.Instance;
    }

    private static Option<long> NextOffset(string json) =>
        Try.Run(() => JsonDocument.Parse(json)).Match(
            document => document.RootElement.TryGetProperty("nextExpectedRanges", out var ranges) && ranges.GetArrayLength() > 0 && long.TryParse(ranges[0].GetString()?.Split('-')[0], NumberStyles.None, CultureInfo.InvariantCulture, out var next) ? Option.Some(next) : Option.None<long>(),
            _ => Option.None<long>());

    private static string ContentRange(long start, long length, long total) => string.Create(CultureInfo.InvariantCulture, $"bytes {start}-{start + length - 1}/{total}");

    private static void WriteFileSystemInfo(System.Text.Json.Utf8JsonWriter writer, DateTime lastWriteUtc)
    {
        writer.WriteStartObject("fileSystemInfo");
        writer.WriteString("lastModifiedDateTime", OneDriveTransferHttp.Timestamp(lastWriteUtc));
        writer.WriteEndObject();
    }

    private static Result<UploadedFile, ProviderError> ToUploaded(string json, long fallbackSize, string hash) =>
        Try.Run(() => JsonDocument.Parse(json)).Match(
            document => ToUploaded(document.RootElement, fallbackSize, hash),
            _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()));

    private static Result<UploadedFile, ProviderError> ToUploaded(JsonElement item, long fallbackSize, string hash)
    {
        var size = item.TryGetProperty("size", out var length) && length.ValueKind is JsonValueKind.Number ? length.GetInt64() : fallbackSize;

        return RemoteItemIdFactory.Create(TextOf(item, "id")).Match(
            id => ContentTagFactory.Create(TextOf(item, "cTag")).Match(
                cTag => EntityTagFactory.Create(TextOf(item, "eTag")).Match(
                    eTag => ContentHashFactory.Create(hash).Match(contentHash => Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(id, cTag, eTag, size, contentHash)), _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
                    _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
                _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
            _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
    }

    private static Option<string> StringOf(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String ? Option.Some(value.GetString() ?? string.Empty) : Option.None<string>();

    private static string TextOf(JsonElement item, string name) => StringOf(item, name).Match(text => text, () => string.Empty);

    private static bool Changed(IFileInfo source, long length, DateTime lastWriteUtc)
    {
        source.Refresh();

        return !source.Exists || source.Length != length || source.LastWriteTimeUtc != lastWriteUtc;
    }

    private static Option<string> TagOf(UploadPrecondition precondition) => precondition is UploadPrecondition.MustMatch match ? Option.Some(match.CTag.Value) : Option.None<string>();

    private Task<Result<UploadedFile, ProviderError>> RunAsync(Transfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        transfer.Source.Refresh();
        transfer.Length = transfer.Source.Length;
        transfer.LastWriteUtc = transfer.Source.LastWriteTimeUtc;

        return transfer.Length == 0 ? UploadEmptyAsync(transfer, credentials, cancellationToken) : UploadInFragmentsAsync(transfer, credentials, cancellationToken);
    }

    private async Task<Result<UploadedFile, ProviderError>> UploadEmptyAsync(Transfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        var url = transfer.Target switch
        {
            UploadTarget.ReplacingItem replacing => OneDriveTransferHttp.ItemUri(credentials, Uri.EscapeDataString(replacing.ItemId.Value), "/content"),
            UploadTarget.NewFileIn created => OneDriveTransferHttp.ChildUri(credentials, OneDriveTransferHttp.ParentSegment(created.Parent), transfer.Source.Name, ":/content?@microsoft.graph.conflictBehavior=fail"),
            _ => OneDriveTransferHttp.ItemUri(credentials, "root", "/content"),
        };
        using var put = OneDriveTransferHttp.Authorized(HttpMethod.Put, url, credentials);
        put.Content = new ByteArrayContent([]);
        _ = TagOf(transfer.Precondition).Match(tag => Matched(put, tag), () => Unit.Instance);

        return await http.SendAsync(transfer.AccountId, put, cancellationToken)
            .BindAsync(response => ReadEmptyCreatedAsync(transfer, credentials, response, cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task<Result<UploadedFile, ProviderError>> ReadEmptyCreatedAsync(Transfer transfer, TransferCredentials credentials, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (!response.IsSuccessStatusCode)
            return await FailedAsync(response, cancellationToken).ConfigureAwait(false);

        var created = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var id = Try.Run(() => JsonDocument.Parse(created)).Match(document => StringOf(document.RootElement, "id"), _ => Option.None<string>());

        return await id.Match(found => PatchAsync(transfer, credentials, found, cancellationToken), () => Task.FromResult(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()))).ConfigureAwait(false);
    }

    private async Task<Result<UploadedFile, ProviderError>> PatchAsync(Transfer transfer, TransferCredentials credentials, string id, CancellationToken cancellationToken)
    {
        using var patch = OneDriveTransferHttp.Authorized(HttpMethod.Patch, OneDriveTransferHttp.ItemUri(credentials, Uri.EscapeDataString(id)), credentials);
        patch.Content = OneDriveTransferHttp.JsonBody(writer =>
        {
            writer.WriteStartObject();
            WriteFileSystemInfo(writer, transfer.LastWriteUtc);
            writer.WriteEndObject();
        });

        return await http.SendAsync(transfer.AccountId, patch, cancellationToken).BindAsync(patched => ReadPatchedAsync(patched, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<UploadedFile, ProviderError>> ReadPatchedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (!response.IsSuccessStatusCode)
            return await FailedAsync(response, cancellationToken).ConfigureAwait(false);

        OneDriveTransferLog.UploadCompleted(logger, 0, 0);

        return ToUploaded(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), 0, EmptyHash());
    }

    private async Task<Result<UploadedFile, ProviderError>> FailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var (error, status, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.CallFailed(logger, status, code);

        return Result.Failure<UploadedFile, ProviderError>(error);
    }

    private async Task<Result<UploadedFile, ProviderError>> UploadInFragmentsAsync(Transfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        var created = await CreateSessionAsync(transfer, credentials, cancellationToken).ConfigureAwait(false);

        return await created.Match(_ => SendFragmentsAsync(transfer, credentials, cancellationToken), error => Task.FromResult(Result.Failure<UploadedFile, ProviderError>(error))).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> CreateSessionAsync(Transfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        var url = transfer.Target switch
        {
            UploadTarget.ReplacingItem replacing => OneDriveTransferHttp.ItemUri(credentials, Uri.EscapeDataString(replacing.ItemId.Value), "/createUploadSession"),
            UploadTarget.NewFileIn created => OneDriveTransferHttp.ChildUri(credentials, OneDriveTransferHttp.ParentSegment(created.Parent), transfer.Source.Name, ":/createUploadSession"),
            _ => OneDriveTransferHttp.ItemUri(credentials, "root", "/createUploadSession"),
        };
        using var request = OneDriveTransferHttp.Authorized(HttpMethod.Post, url, credentials);
        request.Content = OneDriveTransferHttp.JsonBody(writer =>
        {
            writer.WriteStartObject();
            writer.WriteStartObject("item");
            if (transfer.Target is UploadTarget.NewFileIn)
                writer.WriteString("@microsoft.graph.conflictBehavior", "fail");

            WriteFileSystemInfo(writer, transfer.LastWriteUtc);
            writer.WriteEndObject();
            writer.WriteEndObject();
        });
        _ = TagOf(transfer.Precondition).Match(tag => Matched(request, tag), () => Unit.Instance);

        return await http.SendAsync(transfer.AccountId, request, cancellationToken).BindAsync(response => ReadSessionAsync(transfer, response, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> ReadSessionAsync(Transfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (!response.IsSuccessStatusCode)
        {
            var (error, status, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
            OneDriveTransferLog.CallFailed(logger, status, code);

            return Result.Failure<Unit, ProviderError>(error);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var address = Try.Run(() => JsonDocument.Parse(json)).Match(document => StringOf(document.RootElement, "uploadUrl"), _ => Option.None<string>()).Match(text => text, () => string.Empty);
        if (!Uri.TryCreate(address, UriKind.Absolute, out var session) || session.Scheme != Uri.UriSchemeHttps)
            return Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateUnexpected());

        transfer.Session = Option.Some(session);
        transfer.Offset = 0;
        transfer.HashedUpTo = 0;
        transfer.Hash.GetHashAndReset();

        return Result.Success<Unit, ProviderError>(Unit.Instance);
    }

    private async Task<Result<UploadedFile, ProviderError>> SendFragmentsAsync(Transfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
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

    private static async Task<Result<byte[], ProviderError>> ReadFragmentAsync(Transfer transfer, long length, CancellationToken cancellationToken) =>
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

    private async Task<Option<Result<UploadedFile, ProviderError>>> PutFragmentAsync(Transfer transfer, TransferCredentials credentials, byte[] buffer, CancellationToken cancellationToken)
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

    private async Task<Option<Result<UploadedFile, ProviderError>>> ReadFragmentResponseAsync(Transfer transfer, TransferCredentials credentials, HttpResponseMessage response, CancellationToken cancellationToken)
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

    private async Task<Result<UploadedFile, ProviderError>> CompleteAsync(Transfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var hash = transfer.HashedUpTo == transfer.Length ? HexOf(transfer.Hash) : await HashAgainAsync(transfer, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.UploadCompleted(logger, transfer.Length, transfer.Fragments + 1);

        return ToUploaded(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), transfer.Length, hash);
    }

    private static async Task<string> HashAgainAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await using var stream = transfer.Source.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<Option<Result<UploadedFile, ProviderError>>> AdvanceAsync(Transfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var next = NextOffset(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        transfer.Fragments++;

        return next.Match(
            offset => Advanced(transfer, offset),
            () => Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())));
    }

    private static Option<Result<UploadedFile, ProviderError>> Advanced(Transfer transfer, long offset)
    {
        transfer.Attempts = offset > transfer.Offset ? 0 : transfer.Attempts + 1;
        transfer.Offset = offset;
        transfer.Progress.Report(TransferProgressFactory.Create(offset, transfer.Length));

        return transfer.Attempts > MaxAttempts
            ? Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient()))
            : Option.None<Result<UploadedFile, ProviderError>>();
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> ResumeAsync(Transfer transfer, ProviderError cause, CancellationToken cancellationToken)
    {
        transfer.Attempts++;
        if (transfer.Attempts > MaxAttempts)
            return Option.Some(await AbandonAsync(transfer, cause, cancellationToken).ConfigureAwait(false));

        var session = transfer.Session.Match(found => found, () => throw new InvalidOperationException("The session is created before fragments are sent."));
        using var status = OneDriveTransferHttp.Anonymous(HttpMethod.Get, session);
        var sent = await http.SendAsync(transfer.AccountId, status, cancellationToken).ConfigureAwait(false);

        return await sent.Match(response => ReadStatusAsync(transfer, response, cancellationToken), error => Task.FromResult(Option.Some(Result.Failure<UploadedFile, ProviderError>(error)))).ConfigureAwait(false);
    }

    private async Task<Option<Result<UploadedFile, ProviderError>>> ReadStatusAsync(Transfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
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

    private async Task<Option<Result<UploadedFile, ProviderError>>> RestartAsync(Transfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
    {
        transfer.Restarts++;
        if (transfer.Restarts > MaxRestarts)
            return Option.Some(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient()));

        OneDriveTransferLog.SessionRestarted(logger);
        var created = await CreateSessionAsync(transfer, credentials, cancellationToken).ConfigureAwait(false);

        return created.Match(_ => Option.None<Result<UploadedFile, ProviderError>>(), error => Option.Some(Result.Failure<UploadedFile, ProviderError>(error)));
    }

    private async Task<Result<UploadedFile, ProviderError>> AbandonAsync(Transfer transfer, Action<ILogger> log, CancellationToken cancellationToken)
    {
        log(logger);
        await DeleteSessionAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateSourceChanged());
    }

    private async Task<Result<UploadedFile, ProviderError>> AbandonAsync(Transfer transfer, ProviderError error, CancellationToken cancellationToken)
    {
        await DeleteSessionAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Failure<UploadedFile, ProviderError>(error);
    }

    private async Task CancelSessionAsync(Transfer transfer)
    {
        OneDriveTransferLog.UploadCancelled(logger);
        await DeleteSessionAsync(transfer, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task DeleteSessionAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await transfer.Session.Match(session => DeleteAsync(transfer.AccountId, session, cancellationToken), () => Task.CompletedTask).ConfigureAwait(false);
    }

    private async Task DeleteAsync(AccountId accountId, Uri session, CancellationToken cancellationToken)
    {
        using var request = OneDriveTransferHttp.Anonymous(HttpMethod.Delete, session);
        using var outcome = (await http.SendAsync(accountId, request, cancellationToken).ConfigureAwait(false)).Match(response => response, _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }

    private sealed class Transfer(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress)
    {
        internal AccountId AccountId { get; } = accountId;

        internal UploadTarget Target { get; } = target;

        internal IFileInfo Source { get; } = source;

        internal UploadPrecondition Precondition { get; } = precondition;

        internal IProgress<TransferProgress> Progress { get; } = progress;

        internal Option<Uri> Session { get; set; } = Option.None<Uri>();

        internal long Length { get; set; }

        internal DateTime LastWriteUtc { get; set; }

        internal long Offset { get; set; }

        internal long HashedUpTo { get; set; }

        internal IncrementalHash Hash { get; } = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        internal int Fragments { get; set; }

        internal int Attempts { get; set; }

        internal int Restarts { get; set; }
    }
}
