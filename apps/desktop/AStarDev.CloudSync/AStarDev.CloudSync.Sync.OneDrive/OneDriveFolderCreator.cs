using System.Net;
using System.Text.Json;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="logger">Reports status codes only, never names.</param>
internal sealed class OneDriveFolderCreator(OneDriveTransferHttp http, ILogger<OneDriveFolderCreator> logger) : IRemoteFolderCreator
{
    /// <inheritdoc />
    public async Task<Result<RemoteFolder, ProviderError>> EnsureFolderAsync(AccountId accountId, Option<RemoteItemId> parent, string name, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await http.CredentialsAsync(accountId, outcome => OneDriveTransferLog.TokenUnavailable(logger, outcome), cancellationToken)
            .BindAsync(credentials => CreateAsync(accountId, credentials, parent, name, cancellationToken))
            .ConfigureAwait(false);
    }

    private static Result<RemoteFolder, ProviderError> ToFolder(string json, string requestedParent, string requestedName) =>
        Try.Run(() => JsonDocument.Parse(json)).Match(
            document => ToFolder(document.RootElement, requestedParent, requestedName),
            _ => Result.Failure<RemoteFolder, ProviderError>(ProviderErrorFactory.CreateUnexpected()));

    private static Result<RemoteFolder, ProviderError> ToFolder(JsonElement item, string requestedParent, string requestedName)
    {
        if (!item.TryGetProperty("folder", out _))
            return Result.Failure<RemoteFolder, ProviderError>(ProviderErrorFactory.CreateNameConflict());

        var parent = item.TryGetProperty("parentReference", out var reference) && reference.TryGetProperty("id", out var parentId) ? parentId.GetString() : requestedParent;
        var children = item.GetProperty("folder").TryGetProperty("childCount", out var count) && count.GetInt32() > 0 ? RemoteFolderChildrenFactory.CreateMayHaveChildFolders() : RemoteFolderChildrenFactory.CreateNoChildFolders();

        return RemoteItemIdFactory.Create(item.TryGetProperty("id", out var id) ? id.GetString() : null).Match(
            folderId => RemoteItemIdFactory.Create(parent).Match(parentItemId => Result.Success<RemoteFolder, ProviderError>(RemoteFolderFactory.Create(folderId, requestedName, parentItemId, children)), _ => Result.Failure<RemoteFolder, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
            _ => Result.Failure<RemoteFolder, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
    }

    private async Task<Result<RemoteFolder, ProviderError>> CreateAsync(AccountId accountId, TransferCredentials credentials, Option<RemoteItemId> parent, string name, CancellationToken cancellationToken)
    {
        var segment = OneDriveTransferHttp.ParentSegment(parent);
        using var request = OneDriveTransferHttp.Authorized(HttpMethod.Post, OneDriveTransferHttp.ItemUri(credentials, segment, "/children"), credentials);
        request.Content = OneDriveTransferHttp.JsonBody(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("name", name);
            writer.WriteStartObject("folder");
            writer.WriteEndObject();
            writer.WriteString("@microsoft.graph.conflictBehavior", "fail");
            writer.WriteEndObject();
        });

        return await http.SendAsync(accountId, request, cancellationToken).BindAsync(response => ReadCreatedAsync(accountId, credentials, response, segment, name, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<RemoteFolder, ProviderError>> ReadCreatedAsync(AccountId accountId, TransferCredentials credentials, HttpResponseMessage response, string segment, string name, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (response.IsSuccessStatusCode)
            return ToFolder(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), segment, name);

        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
            return await LookupAsync(accountId, credentials, segment, name, cancellationToken).ConfigureAwait(false);

        return await FailedAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<RemoteFolder, ProviderError>> LookupAsync(AccountId accountId, TransferCredentials credentials, string segment, string name, CancellationToken cancellationToken)
    {
        using var request = OneDriveTransferHttp.Authorized(HttpMethod.Get, OneDriveTransferHttp.ChildUri(credentials, segment, name), credentials);

        return await http.SendAsync(accountId, request, cancellationToken).BindAsync(response => ReadLookupAsync(response, segment, name, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<RemoteFolder, ProviderError>> ReadLookupAsync(HttpResponseMessage response, string segment, string name, CancellationToken cancellationToken)
    {
        using var owned = response;

        return response.IsSuccessStatusCode
            ? ToFolder(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), segment, name)
            : await FailedAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<RemoteFolder, ProviderError>> FailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var (error, status, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.CallFailed(logger, status, code);

        return Result.Failure<RemoteFolder, ProviderError>(error);
    }
}
