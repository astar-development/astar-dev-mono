using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="logger">Reports status codes only, never names.</param>
internal sealed class OneDriveItemRemover(OneDriveTransferHttp http, ILogger<OneDriveItemRemover> logger) : IRemoteItemRemover
{
    /// <inheritdoc />
    public async Task<Result<Unit, ProviderError>> RemoveAsync(AccountId accountId, RemoteItemId itemId, RemovePrecondition precondition, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await http.CredentialsAsync(accountId, outcome => OneDriveTransferLog.TokenUnavailable(logger, outcome), cancellationToken)
            .BindAsync(credentials => DeleteAsync(accountId, credentials, itemId, precondition, cancellationToken))
            .ConfigureAwait(false);
    }

    private static string TagOf(RemovePrecondition precondition) => precondition switch
    {
        RemovePrecondition.MustMatchContent content => content.CTag.Value,
        RemovePrecondition.MustMatch entity => entity.ETag.Value,
        _ => string.Empty,
    };

    private async Task<Result<Unit, ProviderError>> DeleteAsync(AccountId accountId, TransferCredentials credentials, RemoteItemId itemId, RemovePrecondition precondition, CancellationToken cancellationToken)
    {
        using var request = OneDriveTransferHttp.Authorized(HttpMethod.Delete, OneDriveTransferHttp.ItemUri(credentials, Uri.EscapeDataString(itemId.Value)), credentials);
        OneDriveTransferHttp.AddIfMatch(request, TagOf(precondition));

        return await http.SendAsync(accountId, request, cancellationToken).BindAsync(response => ReadAsync(response, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ProviderError>> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var owned = response;
        if (response.IsSuccessStatusCode)
            return Result.Success<Unit, ProviderError>(Unit.Instance);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            OneDriveTransferLog.AlreadyGone(logger);

            return Result.Success<Unit, ProviderError>(Unit.Instance);
        }

        var (error, status, code) = await http.ErrorOfAsync(response, cancellationToken).ConfigureAwait(false);
        OneDriveTransferLog.CallFailed(logger, status, code);

        return Result.Failure<Unit, ProviderError>(error);
    }
}
