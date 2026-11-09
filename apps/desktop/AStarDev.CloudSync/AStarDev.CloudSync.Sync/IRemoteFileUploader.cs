using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Uploads one local file; the provider chooses a simple or a session upload (#1401). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteFileUploader
{
    /// <summary>Uploads <paramref name="source"/> into <paramref name="parentFolder"/> if <paramref name="precondition"/> holds.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="parentFolder">The remote folder to upload into.</param>
    /// <param name="source">The local file.</param>
    /// <param name="precondition">What must be true remotely; otherwise <see cref="ProviderError.PreconditionFailed"/>.</param>
    /// <param name="progress">Receives transfer progress.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, RemoteItemId parentFolder, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken);
}
