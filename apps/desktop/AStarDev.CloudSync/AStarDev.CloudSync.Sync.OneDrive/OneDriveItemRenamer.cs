using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="http">Sends the requests through the account throttle gate.</param>
/// <param name="logger">Reports status codes only, never names.</param>
internal sealed class OneDriveItemRenamer(OneDriveTransferHttp http, ILogger<OneDriveItemRenamer> logger) : IRemoteItemRenamer
{
    /// <inheritdoc />
    public Task<Result<Unit, ProviderError>> RenameAsync(AccountId accountId, RemoteItemId itemId, string newName, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{http}{logger}{accountId}{itemId}{newName}{cancellationToken}");
}
