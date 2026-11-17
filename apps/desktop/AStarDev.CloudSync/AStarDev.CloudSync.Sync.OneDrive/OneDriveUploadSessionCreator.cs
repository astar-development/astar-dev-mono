using System.Text.Json;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using static AStarDev.CloudSync.Sync.OneDrive.OneDriveUploadSupport;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Asks Graph for a new upload session and stores it on the transfer.</summary>
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="keeper">Saves the new session so the upload can continue after a restart.</param>
/// <param name="logger">Reports counts and status codes only, never names or URLs.</param>
internal sealed class OneDriveUploadSessionCreator(OneDriveTransferHttp http, OneDriveUploadSessionKeeper keeper, ILogger<OneDriveUploadSessionCreator> logger)
{
    internal async Task<Result<Unit, ProviderError>> CreateSessionAsync(UploadTransfer transfer, TransferCredentials credentials, CancellationToken cancellationToken)
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

    private async Task<Result<Unit, ProviderError>> ReadSessionAsync(UploadTransfer transfer, HttpResponseMessage response, CancellationToken cancellationToken)
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
        transfer.ExpiresAt = ExpiryOf(json);
        transfer.Offset = 0;
        transfer.HashedUpTo = 0;
        transfer.Hash.GetHashAndReset();
        await keeper.SaveProgressAsync(transfer, cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ProviderError>(Unit.Instance);
    }
}
