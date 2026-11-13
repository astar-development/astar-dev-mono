using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="graphAccess">Finds the drive and the access token.</param>
/// <param name="httpClients">Creates the HTTP client that goes through the account throttle gate.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports counts and status codes only.</param>
internal sealed class OneDriveFolderCreator(OneDriveGraphAccess graphAccess, IHttpClientFactory httpClients, TimeProvider timeProvider, ILogger<OneDriveFolderCreator> logger) : IRemoteFolderCreator
{
    /// <inheritdoc />
    public Task<Result<RemoteFolder, ProviderError>> EnsureFolderAsync(AccountId accountId, Option<RemoteItemId> parent, string name, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{graphAccess}{httpClients}{timeProvider}{logger}{accountId}{parent}{name}{cancellationToken}");
}
