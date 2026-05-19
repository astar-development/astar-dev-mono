using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class DownloadWorkerFactory(IHttpDownloader downloader, IGraphService graphService, ISyncRepository syncRepository, IFileSystem fileSystem) : IDownloadWorkerFactory
{
    /// <inheritdoc />
    public IDownloadWorker Create(int workerId) => new DownloadWorker(workerId, downloader, graphService, syncRepository, fileSystem);
}
