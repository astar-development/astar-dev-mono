using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenADownloadWorkerFactory
{
    private readonly IHttpDownloader _downloader = Substitute.For<IHttpDownloader>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    private DownloadWorkerFactory CreateSut() => new(_downloader, _graphService, _syncRepository, _fileSystem);

    [Fact]
    public void when_create_is_called_then_returns_non_null_worker()
    {
        var worker = CreateSut().Create(1);

        worker.ShouldNotBeNull();
    }

    [Fact]
    public void when_create_is_called_then_returns_download_worker_instance()
    {
        var worker = CreateSut().Create(1);

        worker.ShouldBeOfType<DownloadWorker>();
    }

    [Fact]
    public void when_create_is_called_with_different_ids_then_returns_distinct_instances()
    {
        var sut = CreateSut();

        var worker1 = sut.Create(1);
        var worker2 = sut.Create(2);

        worker1.ShouldNotBeSameAs(worker2);
    }
}
