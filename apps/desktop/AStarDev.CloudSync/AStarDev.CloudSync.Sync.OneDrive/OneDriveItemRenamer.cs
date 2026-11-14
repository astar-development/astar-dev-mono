using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="logger">Reports status codes only, never names.</param>
internal sealed class OneDriveItemRenamer(OneDriveTransferHttp http, ILogger<OneDriveItemRenamer> logger) : IRemoteItemRenamer
{
    private const string NameProperty = "name";

    /// <inheritdoc />
    public async Task<Result<Unit, ProviderError>> RenameAsync(AccountId accountId, RemoteItemId itemId, string newName, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await http.CredentialsAsync(accountId, outcome => OneDriveTransferLog.TokenUnavailable(logger, outcome), cancellationToken)
            .BindAsync(credentials => PatchAsync(accountId, credentials, itemId, newName, cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> PatchAsync(AccountId accountId, TransferCredentials credentials, RemoteItemId itemId, string newName, CancellationToken cancellationToken)
    {
        using var request = OneDriveTransferHttp.Authorized(HttpMethod.Patch, OneDriveTransferHttp.ItemUri(credentials, Uri.EscapeDataString(itemId.Value)), credentials);
        request.Content = OneDriveTransferHttp.JsonBody(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString(NameProperty, newName);
            writer.WriteEndObject();
        });

        return await http.SendAsync(accountId, request, cancellationToken).BindAsync(response => ReadAsync(response, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (response.IsSuccessStatusCode)
            return Result.Success<Unit, ProviderError>(Unit.Instance);

        var (error, status, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.CallFailed(logger, status, code);

        return Result.Failure<Unit, ProviderError>(error);
    }
}
