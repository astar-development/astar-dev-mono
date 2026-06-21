using System.Collections.Concurrent;
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
    private const string ColourUploadLocalPath = "/sync-root/a/b/c/d/e/f/g/Photos/black-dress.jpg";
    private const string ColourUploadRelativePath = "a/b/c/d/e/f/g/Photos/black-dress.jpg";

    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ISyncPipeline _pipeline = Substitute.For<ISyncPipeline>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly ISyncedItemRegistrar _syncedItemRegistrar = Substitute.For<ISyncedItemRegistrar>();

    private readonly OneDriveAccount _account = new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com")
    };

    public GivenASyncJobExecutor()
    {
        _settingsService.Current.Returns(new AppSettings());

        _pipeline.RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await foreach (var _ in call.Arg<IAsyncEnumerable<SyncJob>>().ConfigureAwait(false))
                { }
                return 0;
            });
    }

    private SyncJobExecutor CreateSut() => new(_syncRepository, _pipeline, _settingsService, _syncedItemRegistrar);

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
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, EmptyJobStream(), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _pipeline.DidNotReceive().RunAsync(Arg.Any<IAsyncEnumerable<SyncJob>>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_jobs_are_provided_then_sync_repository_enqueue_job_async_is_never_called()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().EnqueueJobAsync(Arg.Any<SyncJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_fewer_than_batch_size_jobs_are_provided_then_enqueue_jobs_is_called_once()
    {
        var jobs = Enumerable.Range(1, 5).Select(i => MakeJob($"item-{i}", SyncDirection.Download)).ToArray();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(jobs), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).EnqueueJobsAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_exactly_batch_size_jobs_are_provided_then_enqueue_jobs_is_called_once()
    {
        var jobs = Enumerable.Range(1, 100).Select(i => MakeJob($"item-{i}", SyncDirection.Download)).ToArray();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(jobs), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).EnqueueJobsAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_more_than_batch_size_jobs_are_provided_then_enqueue_jobs_is_called_multiple_times()
    {
        var jobs = Enumerable.Range(1, 101).Select(i => MakeJob($"item-{i}", SyncDirection.Download)).ToArray();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(jobs), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRepository.Received(2).EnqueueJobsAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_250_jobs_are_provided_then_enqueue_jobs_is_called_three_times()
    {
        var jobs = Enumerable.Range(1, 250).Select(i => MakeJob($"item-{i}", SyncDirection.Download)).ToArray();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(jobs), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRepository.Received(3).EnqueueJobsAsync(Arg.Any<IEnumerable<SyncJob>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_successfully_then_register_download_is_called()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterDownloadAsync(Arg.Any<AccountId>(), Arg.Any<SyncJob>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_successfully_then_register_upload_is_not_called()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.DidNotReceive().RegisterUploadAsync(Arg.Any<AccountId>(), Arg.Any<UploadSyncJob>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_with_mappings_then_mappings_are_forwarded_to_registrar()
    {
        IReadOnlyList<FileClassificationCategory> mappings =
        [
            ((Result<FileClassificationCategory, string>.Ok)FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>())).Value
        ];
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), mappings, _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterDownloadAsync(Arg.Any<AccountId>(), Arg.Any<SyncJob>(), Arg.Any<string>(), Arg.Is<IReadOnlyList<FileClassificationCategory>>(m => m.Count == 1 && m[0].Name == "Documents"), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_download_job_then_normalised_remote_path_is_forwarded_to_registrar()
    {
        var job = MakeJob("item-1", SyncDirection.Download, relativePath: "Photos/red-car.jpg");
        SimulateJobCompleted(job.Complete());
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterDownloadAsync(Arg.Any<AccountId>(), Arg.Any<SyncJob>(), "/Photos/red-car.jpg", Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_with_remote_id_then_register_upload_is_called_with_uploaded_id()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(UploadFilePath).Which(m => m.HasStringContent("data"));
        var job = (UploadSyncJob)MakeJob("item-1", SyncDirection.Upload, UploadFilePath);
        SimulateJobCompleted(((UploadSyncJob)job.Complete()) with { UploadedRemoteItemId = "uploaded-remote-id" });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterUploadAsync(Arg.Any<AccountId>(), Arg.Any<UploadSyncJob>(), "uploaded-remote-id", Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_then_register_download_is_not_called()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(UploadFilePath).Which(m => m.HasStringContent("data"));
        var job = (UploadSyncJob)MakeJob("item-1", SyncDirection.Upload, UploadFilePath);
        SimulateJobCompleted(((UploadSyncJob)job.Complete()) with { UploadedRemoteItemId = "uploaded-remote-id" });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.DidNotReceive().RegisterDownloadAsync(Arg.Any<AccountId>(), Arg.Any<SyncJob>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_pipeline_completes_upload_job_with_mappings_then_mappings_are_forwarded_to_registrar()
    {
        IReadOnlyList<FileClassificationCategory> mappings =
        [
            ((Result<FileClassificationCategory, string>.Ok)FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(), "Photos", 1, false, false, Option.None<FileClassificationCategoryId>())).Value
        ];
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(ColourUploadLocalPath).Which(m => m.HasStringContent("data"));
        var job = (UploadSyncJob)MakeJob("item-1", SyncDirection.Upload, localPath: ColourUploadLocalPath, relativePath: ColourUploadRelativePath);
        SimulateJobCompleted(((UploadSyncJob)job.Complete()) with { UploadedRemoteItemId = "uploaded-remote-id" });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), mappings, _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterUploadAsync(Arg.Any<AccountId>(), Arg.Any<UploadSyncJob>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Is<IReadOnlyList<FileClassificationCategory>>(m => m.Count == 1 && m[0].Name == "Photos"), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_multiple_download_jobs_succeed_then_register_download_is_called_for_each()
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
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job1, job2), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(2).RegisterDownloadAsync(Arg.Any<AccountId>(), Arg.Any<SyncJob>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_job_fails_then_register_download_is_not_called()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Fail("disk full"));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.DidNotReceive().RegisterDownloadAsync(Arg.Any<AccountId>(), Arg.Any<SyncJob>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_job_fails_then_register_upload_is_not_called()
    {
        var job = MakeJob("item-1", SyncDirection.Download);
        SimulateJobCompleted(job.Fail("disk full"));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], _ => { }, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.DidNotReceive().RegisterUploadAsync(Arg.Any<AccountId>(), Arg.Any<UploadSyncJob>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
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
        var sut = CreateSut();

        await sut.ExecuteAsync(_account, tokenFactory, JobStream(job), new ConcurrentDictionary<string, SyncedItemEntity>(), [], args => progressReceived.Add(args), _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        progressReceived.ShouldHaveSingleItem();
        progressReceived[0].CurrentFile.ShouldBe("file.txt");
    }
}
