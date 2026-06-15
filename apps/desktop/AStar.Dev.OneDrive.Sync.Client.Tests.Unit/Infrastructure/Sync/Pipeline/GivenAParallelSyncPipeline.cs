using System.Collections.Concurrent;
using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenAParallelSyncPipeline
{
    private const string AccountIdValue = "account-1";
    private const string FolderIdValue  = "folder-1";

    private readonly ISyncWorkerFactory _workerFactory = Substitute.For<ISyncWorkerFactory>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();

    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("test-token");

    private static IOptions<SyncSettings> SyncSettingsOptions
        => Options.Create(new SyncSettings { ProgressReportInterval = 100, MaxConcurrentDownloads = 4 });

    public GivenAParallelSyncPipeline()
    {
        _workerFactory.Create(Arg.Any<int>()).Returns(_ => new SucceedingDownloadWorker());
    }

    private ParallelSyncPipeline CreateSut() => new(_workerFactory, _syncRepository, Substitute.For<ILogger<ParallelSyncPipeline>>(), SyncSettingsOptions);

    private static DownloadSyncJob MakeDownloadJob(string relativePath = "folder/file.txt")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("/tmp/test-file.txt", relativePath);
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private static async IAsyncEnumerable<SyncJob> EmptyJobStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<SyncJob> JobStream(params SyncJob[] jobs)
    {
        foreach (var job in jobs)
        {
            await Task.CompletedTask;
            yield return job;
        }
    }

    [Fact]
    public async Task when_job_list_is_empty_then_on_progress_is_never_called()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(EmptyJobStream(), TokenFactory, progressEvents.Add, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        progressEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_job_list_is_empty_then_on_job_completed_is_never_called()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(EmptyJobStream(), TokenFactory, _ => { }, args => { completedEvents.Add(args); return Task.CompletedTask; }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_job_list_is_empty_then_clear_completed_jobs_is_never_called()
    {
        var sut = CreateSut();

        await sut.RunAsync(EmptyJobStream(), TokenFactory, _ => { }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        await _syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<AccountId>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_single_download_job_is_processed_then_on_job_completed_is_called_exactly_once()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, _ => { }, args => { completedEvents.Add(args); return Task.CompletedTask; }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_three_download_jobs_are_processed_then_on_job_completed_is_called_three_times()
    {
        var completedEvents = new ConcurrentBag<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob("a/1.txt"), MakeDownloadJob("a/2.txt"), MakeDownloadJob("a/3.txt")), TokenFactory, _ => { }, args => { completedEvents.Add(args); return Task.CompletedTask; }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_jobs_complete_then_clear_completed_jobs_is_called_exactly_once()
    {
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, _ => { }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ClearCompletedJobsAsync(Arg.Any<AccountId>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_jobs_complete_then_clear_completed_jobs_is_called_with_correct_account_id()
    {
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, _ => { }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).ClearCompletedJobsAsync(new AccountId(AccountIdValue), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_jobs_complete_then_final_progress_event_has_sync_state_idle()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, progressEvents.Add, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        progressEvents[^1].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_jobs_complete_then_final_progress_event_has_empty_current_file()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, progressEvents.Add, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        progressEvents[^1].CurrentFile.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_single_download_job_succeeds_then_on_job_completed_receives_completed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, _ => { }, args => { completedEvents.Add(args); return Task.CompletedTask; }, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        completedEvents[0].Job.Status.State.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public async Task when_single_job_finishes_then_per_job_progress_event_has_sync_state_syncing()
    {
        var perJobProgressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, args =>
        {
            if(args.CurrentFile != string.Empty)
                perJobProgressEvents.Add(args);
        }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: TestContext.Current.CancellationToken);

        perJobProgressEvents.Count.ShouldBe(1);
        perJobProgressEvents[0].SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_two_jobs_run_below_throttle_threshold_then_only_final_per_job_progress_event_fires_as_syncing()
    {
        var perJobProgressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob("a/1.txt"), MakeDownloadJob("a/2.txt")), TokenFactory, args =>
        {
            if(args.CurrentFile != string.Empty)
                perJobProgressEvents.Add(args);
        }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, workerCount: 1, ct: TestContext.Current.CancellationToken);

        perJobProgressEvents.Count.ShouldBe(1);
        perJobProgressEvents[0].SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_token_is_cancelled_before_run_then_clear_completed_jobs_is_never_called()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = CreateSut();

        try
        {
            await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, _ => { }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, ct: cts.Token);
        }
        catch(OperationCanceledException) { }

        await _syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<AccountId>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_worker_factory_is_called_then_one_worker_created_per_worker_count()
    {
        var sut = CreateSut();

        await sut.RunAsync(JobStream(MakeDownloadJob()), TokenFactory, _ => { }, _ => Task.CompletedTask, AccountIdValue, FolderIdValue, workerCount: 3, ct: TestContext.Current.CancellationToken);

        _workerFactory.Received(3).Create(Arg.Any<int>());
    }

    private sealed class SucceedingDownloadWorker : ISyncWorker
    {
        public async Task RunAsync(ChannelReader<SyncJob> reader, string accountId, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncJob, bool, string?, Task> onJobComplete, CancellationToken ct)
        {
            await foreach(var job in reader.ReadAllAsync(ct))
                await onJobComplete(job, true, null);
        }
    }
}
