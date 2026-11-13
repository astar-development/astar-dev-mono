using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Uploads one local file; the provider chooses a simple or a session upload (#1401). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteFileUploader
{
    /// <summary>Uploads <paramref name="source"/> to <paramref name="target"/> if <paramref name="precondition"/> holds. The local copy is read without blocking other writers; if it changes during the upload the upload is cancelled with <see cref="ProviderError.SourceChanged"/>.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="target">Where the file goes: a new file in a folder, or a new version of an existing item.</param>
    /// <param name="source">The local file.</param>
    /// <param name="precondition">What must be true remotely; otherwise <see cref="ProviderError.PreconditionFailed"/>.</param>
    /// <param name="progress">Receives transfer progress.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    Task<Result<UploadedFile, ProviderError>> UploadAsync(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress, CancellationToken cancellationToken);
}
