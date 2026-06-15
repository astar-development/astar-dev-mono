using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncJobExecutor
{
    private const string UploadFilePath = "/sync-root/Documents/file.txt";
    private const string ColourDownloadRelativePath = "a/b/c/d/e/f/g/Photos/red-car.jpg";
    private const string ColourUploadLocalPath = "/sync-root/a/b/c/d/e/f/g/Photos/black-dress.jpg";
    private const string ColourUploadRelativePath = "a/b/c/d/e/f/g/Photos/black-dress.jpg";

    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly ISyncPipeline _pipeline = Substitute.For<ISyncPipeline>();
    private readonly IFileClassificationRepository _classificationRepository = Substitute.For<IFileClassificationRepository>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IFileAutoCategorisor _fileAutoCategorisor = new RuleBasedFileAutoCategorisor();

    private readonly OneDriveAccount _account = new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com")
    };

    public GivenASyncJobExecutor()
    {
        _classificationRepository.GetAllKeywordMappingsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<KeywordMapping>>([]));
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _settingsService.Current.Returns(new AppSettings());

        _pipeline.RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await foreach (var _ in call.Arg<IAsyncEnumerable<SyncJob>>().ConfigureAwait(false))
                { }
            });
    }

    private SyncJobExecutor CreateSut(MockFileSystem mockFileSystem) => new(_syncRepository, _syncedItemRepository, _pipeline, _classificationRepository, mockFileSystem, _settingsService, _fileAutoCategorisor);

    private static SyncJob MakeJob(string remoteId, SyncDirection direction, string localPath = "/tmp/file.txt", string relativePath = "Documents/file.txt")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(""), new OneDriveItemId(remoteId));
        var target = SyncFileTargetFactory.Create(localPath, relativePath);
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));

        return direction switch
        {
            SyncDirection.Download => SyncJobFactory.CreateDownload(remote, target, metadata),
            SyncDirection.Upload   => SyncJobFactory.CreateUpload(remote, target, metadata),
            SyncDirection.Delete   => SyncJobFactory.CreateDelete(remote, target, metadata),
            _                      => SyncJobFactory.CreateDownload(remote, target, metadata)
        };
    }

    private static async IAsyncEnumerable<SyncJob> JobStream(params SyncJob[] jobs)
    {
        foreach (var job in jobs)
        {
            await Task.CompletedTask;
            yield return job;
        }
    }

    private static async IAsyncEnumerable<SyncJob> EmptyJobStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private void SimulateJobCompleted(SyncJob completedJob)
        => _pipeline.When(p => p.RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
                    .Do(call =>
                    {
                        var jobs = call.Arg<IAsyncEnumerable<SyncJob>>();
                        Task.Run(async () =>
                        {
                            await foreach (var _ in jobs.ConfigureAwait(false)) { }
                        }).GetAwaiter().GetResult();
                        call.Arg<Func<JobCompletedEventArgs, Task>>()(new JobCompletedEventArgs(completedJob)).GetAwaiter().GetResult();
                    });

    [Fact]
    public async Task when_jobs_list_is_empty_then_pipeline_is_not_called()
    {
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, EmptyJobStream(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _pipeline.DidNotReceive().RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_jobs_are_provided_then_sync_repository_enqueue_job_is_called_once()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).EnqueueJobAsync(Arg.Any<SyncJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_successfully_then_synced_item_is_upserted()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_successfully_then_classifications_are_upserted()
    {
        const int syncedItemId = 99;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(syncedItemId));
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(syncedItemId, Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_with_matching_rule_then_matched_classification_is_persisted()
    {
        const int syncedItemId = 55;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(syncedItemId));
        IReadOnlyList<KeywordMapping> mappings = [((Result<KeywordMapping, string>.Ok)KeywordMappingFactory.Create("documents", "Documents", Option.None<string>(), Option.None<string>(), false)).Value];
        _classificationRepository.GetAllKeywordMappingsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mappings));
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(syncedItemId, Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Documents")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_with_colour_in_path_then_auto_categoriser_classification_is_persisted()
    {
        const int syncedItemId = 42;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(syncedItemId));
        var job = MakeJob("item-1", SyncDirection.Download, relativePath: ColourDownloadRelativePath);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(syncedItemId, Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Color")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_with_colour_in_path_then_auto_categoriser_classification_is_persisted()
    {
        const int syncedItemId = 43;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(syncedItemId));
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(ColourUploadLocalPath).Which(m => m.HasStringContent("data"));
        var job = (UploadSyncJob)MakeJob("item-1", SyncDirection.Upload, localPath: ColourUploadLocalPath, relativePath: ColourUploadRelativePath);
        SimulateJobCompleted(((UploadSyncJob)job.Complete()) with { UploadedRemoteItemId = "uploaded-remote-id" });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(mockFileSystem);

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(syncedItemId, Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Color")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_with_remote_id_then_synced_item_is_upserted_with_uploaded_id()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(UploadFilePath).Which(m => m.HasStringContent("data"));
        var job = (UploadSyncJob)MakeJob("item-1", SyncDirection.Upload, UploadFilePath);
        SimulateJobCompleted(((UploadSyncJob)job.Complete()) with { UploadedRemoteItemId = "uploaded-remote-id" });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(mockFileSystem);

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "uploaded-remote-id"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_successfully_then_classifications_are_upserted()
    {
        const int syncedItemId = 77;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(syncedItemId));
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(UploadFilePath).Which(m => m.HasStringContent("data"));
        var job = (UploadSyncJob)MakeJob("item-1", SyncDirection.Upload, UploadFilePath);
        SimulateJobCompleted(((UploadSyncJob)job.Complete()) with { UploadedRemoteItemId = "uploaded-remote-id" });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(mockFileSystem);

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(syncedItemId, Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_multiple_jobs_succeed_then_classification_rules_are_fetched_only_once()
    {
        var job1 = MakeJob("item-1", SyncDirection.Download);
        var job2 = MakeJob("item-2", SyncDirection.Download);
        _pipeline.When(p => p.RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var jobs = call.Arg<IAsyncEnumerable<SyncJob>>();
                Task.Run(async () =>
                {
                    await foreach (var _ in jobs.ConfigureAwait(false)) { }
                }).GetAwaiter().GetResult();
                var onJobCompleted = call.Arg<Func<JobCompletedEventArgs, Task>>();
                onJobCompleted(new JobCompletedEventArgs(job1.Complete())).GetAwaiter().GetResult();
                onJobCompleted(new JobCompletedEventArgs(job2.Complete())).GetAwaiter().GetResult();
            });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job1, job2), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _classificationRepository.Received(1).GetAllKeywordMappingsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_job_fails_then_synced_item_is_not_upserted()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Fail("disk full"));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.DidNotReceive().UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_download_job_fails_then_classifications_are_not_upserted()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Fail("disk full"));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.DidNotReceive().UpsertClassificationsAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_jobs_execute_then_on_progress_callback_is_forwarded_from_pipeline()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        _pipeline.When(p => p.RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var jobs = call.Arg<IAsyncEnumerable<SyncJob>>();
                Task.Run(async () =>
                {
                    await foreach (var _ in jobs.ConfigureAwait(false)) { }
                }).GetAwaiter().GetResult();
                call.Arg<Action<SyncProgressEventArgs>>()(new SyncProgressEventArgs("user-1", string.Empty, 1, 1, "file.txt", SyncState.Syncing));
            });

        var progressReceived = new List<SyncProgressEventArgs>();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut(new MockFileSystem());

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), [], args => progressReceived.Add(args), _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        progressReceived.ShouldHaveSingleItem();
        progressReceived[0].CurrentFile.ShouldBe("file.txt");
    }
}
