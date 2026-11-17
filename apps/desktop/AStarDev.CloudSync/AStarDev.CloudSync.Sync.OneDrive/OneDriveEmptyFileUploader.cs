using System.Security.Cryptography;
using System.Text.Json;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using static AStarDev.CloudSync.Sync.OneDrive.OneDriveUploadSupport;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Uploads a zero-byte file with a single request and then sets its modified time.</summary>
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="logger">Reports counts and status codes only, never names or URLs.</param>
internal sealed class OneDriveEmptyFileUploader(OneDriveTransferHttp http, ILogger<OneDriveEmptyFileUploader> logger)
{
    private static string EmptyHash() => Convert.ToHexStringLower(SHA256.HashData([]));

    internal async Task<Result<UploadedFile, ProviderError>> UploadEmptyAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
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

    private async Task<Result<UploadedFile, ProviderError>> ReadEmptyCreatedAsync(UploadTransfer transfer, TransferCredentials credentials, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (!response.IsSuccessStatusCode)
            return await FailedAsync(response, cancellationToken).ConfigureAwait(false);

        var created = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var id = Try.Run(() => JsonDocument.Parse(created)).Match(document => StringOf(document.RootElement, "id"), _ => Option.None<string>());

        return await id.Match(found => PatchAsync(transfer, credentials, found, cancellationToken), () => Task.FromResult(Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()))).ConfigureAwait(false);
    }

    private async Task<Result<UploadedFile, ProviderError>> PatchAsync(UploadTransfer transfer, TransferCredentials credentials, string id, CancellationToken cancellationToken)
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
}
