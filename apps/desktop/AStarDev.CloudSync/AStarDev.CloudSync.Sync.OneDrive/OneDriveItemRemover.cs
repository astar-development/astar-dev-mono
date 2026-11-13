using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="graphAccess">Finds the drive and the access token.</param>
/// <param name="httpClients">Creates the HTTP client that goes through the account throttle gate.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports counts and status codes only.</param>
internal sealed class OneDriveItemRemover(OneDriveGraphAccess graphAccess, IHttpClientFactory httpClients, TimeProvider timeProvider, ILogger<OneDriveItemRemover> logger) : IRemoteItemRemover
{
    /// <inheritdoc />
    public Task<Result<Unit, ProviderError>> RemoveAsync(AccountId accountId, RemoteItemId itemId, RemovePrecondition precondition, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{graphAccess}{httpClients}{timeProvider}{logger}{accountId}{itemId}{precondition}{cancellationToken}");
}
