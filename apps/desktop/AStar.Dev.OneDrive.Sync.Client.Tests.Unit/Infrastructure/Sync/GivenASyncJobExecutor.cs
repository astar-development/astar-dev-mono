using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncJobExecutor
{
    private const string UploadFilePath = "/sync-root/Documents/file.txt";

    private readonly ISyncRepository         _syncRepository         = Substitute.For<ISyncRepository>();
    private readonly ISyncedItemRepository   _syncedItemRepository   = Substitute.For<ISyncedItemRepository>();
    private readonly IParallelDownloadPipeline _pipeline             = Substitute.For<IParallelDownloadPipeline>();

    private readonly OneDriveAccount _account = new()
    {
        Id    = new AccountId("user-1"),
        Email = "user@outlook.com"
    };

    private SyncJobExecutor CreateSut(MockFileSystem mockFs) => new(_syncRepository, _syncedItemRepository, _pipeline, mockFs);

    private static SyncJob MakeJob(string remoteId, SyncDirection direction, string localPath = "/tmp/file.txt")
        => SyncJobFactory.Create(accountId: "user-1", folderId: "", remoteItemId: remoteId, relativePath: "Documents/file.txt", localPath: localPath, direction: direction, fileSize: 100L, remoteModified: DateTimeOffset.UtcNow.AddDays(-1));

    [Fact]
    public async Task when_jobs_list_is_empty_then_pipeline_is_not_called()
    {
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, "token", [], [], _ => { }, _ => { }, TestContext.Current.CancellationToken);

        await _pipeline.DidNotReceive().RunAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<string>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_jobs_are_provided_then_sync_repository_enqueue_is_called()
    {
        var jobs = new List<SyncJob> { MakeJob("item-1", SyncDirection.Download) };
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, "token", jobs, [], _ => { }, _ => { }, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).EnqueueJobsAsync(Arg.Is<IEnumerable<SyncJob>>(j => j.Count() == 1));
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_successfully_then_synced_item_is_upserted()
    {
        var job = MakeJob("item-1", SyncDirection.Download) with { State = SyncJobState.Queued };
        var jobs = new List<SyncJob> { job };

        _pipeline.When(p => p.RunAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<string>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var onJobCompleted = call.Arg<Action<JobCompletedEventArgs>>();
                onJobCompleted(new JobCompletedEventArgs(job with { State = SyncJobState.Completed }));
            });

        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, "token", jobs, [], _ => { }, _ => { }, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_with_remote_id_then_synced_item_is_upserted_with_uploaded_id()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile(UploadFilePath, new MockFileData("data"));

        var job = MakeJob("item-1", SyncDirection.Upload, UploadFilePath);
        var jobs = new List<SyncJob> { job };

        _pipeline.When(p => p.RunAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<string>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var onJobCompleted = call.Arg<Action<JobCompletedEventArgs>>();
                onJobCompleted(new JobCompletedEventArgs(job with { State = SyncJobState.Completed, UploadedRemoteItemId = "uploaded-remote-id" }));
            });

        var sut = CreateSut(mockFs);

        await sut.ExecuteAsync(_account, "token", jobs, [], _ => { }, _ => { }, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "uploaded-remote-id"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_job_fails_then_synced_item_is_not_upserted()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        var jobs = new List<SyncJob> { job };

        _pipeline.When(p => p.RunAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<string>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var onJobCompleted = call.Arg<Action<JobCompletedEventArgs>>();
                onJobCompleted(new JobCompletedEventArgs(job with { State = SyncJobState.Failed, ErrorMessage = "disk full" }));
            });

        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, "token", jobs, [], _ => { }, _ => { }, TestContext.Current.CancellationToken);

        await _syncedItemRepository.DidNotReceive().UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_jobs_execute_then_on_progress_callback_is_forwarded_from_pipeline()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        var jobs = new List<SyncJob> { job };

        _pipeline.When(p => p.RunAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<string>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Action<JobCompletedEventArgs>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var onProgress = call.Arg<Action<SyncProgressEventArgs>>();
                onProgress(new SyncProgressEventArgs("user-1", string.Empty, 1, 1, "file.txt", SyncState.Syncing));
            });

        var progressReceived = new List<SyncProgressEventArgs>();
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, "token", jobs, [], args => progressReceived.Add(args), _ => { }, TestContext.Current.CancellationToken);

        progressReceived.ShouldHaveSingleItem();
        progressReceived[0].CurrentFile.ShouldBe("file.txt");
    }
}
