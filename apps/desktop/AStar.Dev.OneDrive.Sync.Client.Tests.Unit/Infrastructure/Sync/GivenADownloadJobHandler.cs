using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenADownloadJobHandler
{
    private const string AccessToken = "test-token";
    private const string ItemId = "item-abc";

    private readonly IHttpDownloader _downloader = Substitute.For<IHttpDownloader>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();

    public GivenADownloadJobHandler()
    {
        _downloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<global::System.Reactive.Unit, string>.Ok(global::System.Reactive.Unit.Default));
    }

    private DownloadJobHandler CreateSut() => new(_downloader, _graphService);

    private static DownloadSyncJob MakeDownloadJob(string? downloadUrl = "https://example.com/file")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, downloadUrl);
    }

    private static UploadSyncJob MakeUploadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId("folder-1"), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateUpload(remote, target, metadata);
    }

    private static DeleteSyncJob MakeDeleteJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDelete(remote, target, metadata);
    }

    [Fact]
    public void when_given_download_sync_job_then_can_handle_returns_true()
    {
        var job = MakeDownloadJob();

        CreateSut().CanHandle(job).ShouldBeTrue();
    }

    [Fact]
    public void when_given_upload_sync_job_then_can_handle_returns_false()
    {
        var job = MakeUploadJob();

        CreateSut().CanHandle(job).ShouldBeFalse();
    }

    [Fact]
    public void when_given_delete_sync_job_then_can_handle_returns_false()
    {
        var job = MakeDeleteJob();

        CreateSut().CanHandle(job).ShouldBeFalse();
    }

    [Fact]
    public async Task when_job_has_url_then_downloader_called_with_that_url()
    {
        const string url = "https://example.com/direct";
        var job = MakeDownloadJob(url);

        await CreateSut().HandleAsync(job, AccessToken, TestContext.Current.CancellationToken);

        await _downloader.Received(1).DownloadAsync(url, job.Target.LocalPath, job.Metadata.RemoteModified, ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_job_has_no_url_then_graph_service_called_to_resolve_it()
    {
        const string fetchedUrl = "https://example.com/fetched";
        var job = MakeDownloadJob(downloadUrl: null);

        _graphService.GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok(fetchedUrl));

        await CreateSut().HandleAsync(job, AccessToken, TestContext.Current.CancellationToken);

        await _graphService.Received(1).GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>());
        await _downloader.Received(1).DownloadAsync(fetchedUrl, job.Target.LocalPath, job.Metadata.RemoteModified, ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_url_resolution_fails_then_result_is_error_with_message()
    {
        const string errorMessage = "No download URL available.";
        var job = MakeDownloadJob(downloadUrl: null);

        _graphService.GetDownloadUrlAsync(AccessToken, ItemId, Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Error(errorMessage));

        var result = await CreateSut().HandleAsync(job, AccessToken, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeFalse();
        result.Match<string?>(_ => null, error => error).ShouldBe(errorMessage);
    }

    [Fact]
    public async Task when_download_succeeds_then_result_is_ok_with_job()
    {
        var job = MakeDownloadJob();

        var result = await CreateSut().HandleAsync(job, AccessToken, TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_download_fails_then_result_is_error_with_message()
    {
        const string downloadError = "Network timeout";
        var job = MakeDownloadJob();

        _downloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<global::System.Reactive.Unit, string>.Error(downloadError));

        var result = await CreateSut().HandleAsync(job, AccessToken, TestContext.Current.CancellationToken);

        result.Match<string?>(_ => null, error => error).ShouldBe(downloadError);
    }
}
