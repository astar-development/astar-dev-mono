using System.Reactive;
using AStar.Dev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;

public interface IHttpDownloader
{
    /// <summary>
    /// Downloads the file at <paramref name="url"/> to <paramref name="localPath"/>.
    /// Automatically retries on 429 with exponential backoff.
    /// Preserves the remote last-modified timestamp on the local file.
    /// </summary>
    Task<Result<Unit, string>> DownloadAsync(string url, string localPath, DateTimeOffset remoteModified, IProgress<long>? progress = null, CancellationToken cancellationToken = default);
}
