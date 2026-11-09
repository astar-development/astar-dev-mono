using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Downloads one remote file (#1400). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteFileDownloader
{
    /// <summary>Downloads the file's content to <paramref name="target"/>.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="fileId">The remote file.</param>
    /// <param name="target">Where to write the content.</param>
    /// <param name="progress">Receives transfer progress.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    Task<Result<DownloadedFile, ProviderError>> DownloadAsync(AccountId accountId, RemoteItemId fileId, IFileInfo target, IProgress<TransferProgress> progress, CancellationToken cancellationToken);
}
