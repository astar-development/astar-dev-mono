using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="graphAccess">Finds the drive and the access token.</param>
/// <param name="httpClients">Creates the HTTP client that goes through the account throttle gate.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports counts and status codes only.</param>
internal sealed class OneDriveFileUploader(OneDriveGraphAccess graphAccess, IHttpClientFactory httpClients, TimeProvider timeProvider, ILogger<OneDriveFileUploader> logger) : IRemoteFileUploader
{
    /// <inheritdoc />
    public Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, UploadTarget target, System.IO.Abstractions.IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{graphAccess}{httpClients}{timeProvider}{logger}{accountId}{target}{source}{precondition}{progress}{cancellationToken}");
}
