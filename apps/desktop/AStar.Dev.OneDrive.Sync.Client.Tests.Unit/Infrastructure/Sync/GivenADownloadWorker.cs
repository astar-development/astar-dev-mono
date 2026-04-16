using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenADownloadWorker
{
    private const string AccessToken = "test-token";
    private const string ItemId      = "item-abc";

    private readonly IHttpDownloader  _downloader     = Substitute.For<IHttpDownloader>();
    private readonly IGraphService    _graphService   = Substitute.For<IGraphService>();
    private readonly ISyncRepository  _syncRepository = Substitute.For<ISyncRepository>();

    private DownloadWorker CreateSut(int workerId = 1) => new(workerId, _downloader, _graphService, _syncRepository);

    private static SyncJob MakeDownloadJob(string? downloadUrl = "https://example.com/file") => new()
    {
        RemoteItemId   = ItemId,
        RelativePath   = "Desktop/file.txt",
        LocalPath      = "/tmp/file.txt",
        Direction      = SyncDirection.Download,
        DownloadUrl    = downloadUrl,
        RemoteModified = DateTimeOffset.UtcNow
    };

    private static async Task<(List<SyncJob> Completed, List<string?> Errors)> RunWorkerWithJobsAsync(DownloadWorker worker, IEnumerable<SyncJob> jobs, CancellationToken ct)
    {
        var channel   = Channel.CreateUnbounded<SyncJob>();
        var completed = new List<SyncJob>();
        var errors    = new List<string?>();

        foreach(var job in jobs)
            channel.Writer.TryWrite(job);

        channel.Writer.Complete();

        await worker.RunAsync(channel.Reader, AccessToken, (job, _, error) =>
        {
            completed.Add(job);
            errors.Add(error);
        }, ct);

        return (completed, errors);
    }

    [Fact]
    public async Task when_download_job_has_url_then_downloader_is_called_with_that_url()
    {
        const string url = "https://example.com/direct";
        var job = MakeDownloadJob(url);

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _downloader.Received(1).DownloadAsync(url, job.LocalPath, job.RemoteModified, ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_download_job_has_no_url_then_graph_service_is_called_to_resolve_it()
    {
        const string fetchedUrl = "https://example.com/fetched";
        var job = MakeDownloadJob(downloadUrl: null);

        _graphService.GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>())
            .Returns(fetchedUrl);

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _graphService.Received(1).GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>());
        await _downloader.Received(1).DownloadAsync(fetchedUrl, job.LocalPath, job.RemoteModified, ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_download_job_has_no_url_and_graph_returns_null_then_job_fails_with_error_containing_item_id()
    {
        var job = MakeDownloadJob(downloadUrl: null);

        _graphService.GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var (completed, errors) = await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        completed.Count.ShouldBe(1);
        string errorMessage = errors[0].ShouldNotBeNull();
        errorMessage.ShouldContain(ItemId);
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_download_job_has_no_url_and_graph_returns_null_then_job_state_is_set_to_failed()
    {
        var job = MakeDownloadJob(downloadUrl: null);

        _graphService.GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Failed, Arg.Any<string>());
    }

    [Fact]
    public async Task when_download_succeeds_then_job_state_is_set_to_completed()
    {
        var job = MakeDownloadJob();

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Completed);
    }

    [Fact]
    public async Task when_download_succeeds_then_on_job_complete_is_called_with_no_error()
    {
        var job = MakeDownloadJob();

        var (_, errors) = await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        errors.Count.ShouldBe(1);
        errors[0].ShouldBeNull();
    }

    [Fact]
    public async Task when_upload_job_is_processed_then_graph_service_upload_is_called()
    {
        var job = new SyncJob
        {
            RemoteItemId   = ItemId,
            FolderId       = "folder-1",
            RelativePath   = "Desktop/file.txt",
            LocalPath      = "/tmp/file.txt",
            Direction      = SyncDirection.Upload,
            RemoteModified = DateTimeOffset.UtcNow
        };

        _graphService.UploadFileAsync(AccessToken, job.LocalPath, Arg.Any<string>(), job.FolderId, Arg.Any<CancellationToken>())
            .Returns("remote-item-id");

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _graphService.Received(1).UploadFileAsync(AccessToken, job.LocalPath, Arg.Any<string>(), job.FolderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_delete_job_is_processed_then_no_downloader_or_graph_download_calls_are_made()
    {
        var job = new SyncJob
        {
            RemoteItemId   = ItemId,
            RelativePath   = "Desktop/file.txt",
            LocalPath      = "/tmp/nonexistent-file-that-does-not-exist.txt",
            Direction      = SyncDirection.Delete,
            RemoteModified = DateTimeOffset.UtcNow
        };

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), ct: Arg.Any<CancellationToken>());
        await _graphService.DidNotReceive().GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
