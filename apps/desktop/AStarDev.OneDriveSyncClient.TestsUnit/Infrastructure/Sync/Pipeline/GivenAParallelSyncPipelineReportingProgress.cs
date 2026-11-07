using System.Collections.Concurrent;
using System.Threading.Channels;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

public sealed class GivenAParallelSyncPipelineReportingProgress
{
    private const string AccountIdValue = "account-1";
    private const string FolderIdValue = "folder-1";
    private const string FailedJobPrefix = "failed/";

    private readonly ISyncWorkerFactory workerFactory = Substitute.For<ISyncWorkerFactory>();
    private readonly ConcurrentQueue<SyncProgressEventArgs> perJobProgress = new();
    private readonly ConcurrentQueue<JobCompletedEventArgs> completedJobs = new();

    public GivenAParallelSyncPipelineReportingProgress() => workerFactory.Create(Arg.Any<int>()).Returns(_ => new ScriptedSyncWorker());

    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("test-token");

    private static DownloadSyncJob MakeDownloadJob(string relativePath)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("/tmp/test-file.txt", relativePath);
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private static async IAsyncEnumerable<SyncJob> JobStream(params string[] relativePaths)
    {
        foreach (string relativePath in relativePaths)
        {
            await Task.CompletedTask;
            yield return MakeDownloadJob(relativePath);
        }
    }

    private static string[] SucceedingJobs(int count) => [.. Enumerable.Range(1, count).Select(index => $"folder/file{index}.txt")];

    private Task<int> RunAsync(params string[] relativePaths)
    {
        var sut = new ParallelSyncPipeline(workerFactory, Substitute.For<ISyncRepository>(), Substitute.For<ILogger<ParallelSyncPipeline>>(), Options.Create(new SyncSettings { ProgressReportInterval = 100 }));

        return sut.RunAsync(JobStream(relativePaths), TokenFactory, OnProgress, OnJobCompletedAsync, AccountIdValue, FolderIdValue, workerCount: 1, cancellationToken: TestContext.Current.CancellationToken);
    }

    private void OnProgress(SyncProgressEventArgs args)
    {
        if (args.CurrentFile.Length > 0)
            perJobProgress.Enqueue(args);
    }

    private Task OnJobCompletedAsync(JobCompletedEventArgs args)
    {
        completedJobs.Enqueue(args);

        return Task.CompletedTask;
    }

    private int[] PerJobProgressCompletedCounts() => [.. perJobProgress.Select(args => args.Completed)];

    [Fact]
    public async Task when_a_single_job_completes_then_one_per_job_progress_event_fires_with_the_syncing_state()
    {
        _ = await RunAsync(SucceedingJobs(1));

        perJobProgress.ShouldHaveSingleItem().SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_a_job_succeeds_then_on_job_completed_receives_the_completed_state()
    {
        _ = await RunAsync(SucceedingJobs(1));

        completedJobs.ShouldHaveSingleItem().Job.Status.State.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public async Task when_a_job_fails_then_on_job_completed_receives_the_failed_state()
    {
        _ = await RunAsync($"{FailedJobPrefix}file.txt");

        completedJobs.ShouldHaveSingleItem().Job.Status.State.ShouldBe(SyncJobState.Failed);
    }

    [Fact]
    public async Task when_the_last_of_two_jobs_completes_then_the_last_per_job_progress_event_has_the_syncing_state()
    {
        _ = await RunAsync(SucceedingJobs(2));

        perJobProgress.Last().SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_1000_jobs_complete_then_per_job_progress_fires_only_at_every_100th_job()
    {
        _ = await RunAsync(SucceedingJobs(1000));

        PerJobProgressCompletedCounts().ShouldBe([100, 200, 300, 400, 500, 600, 700, 800, 900, 1000]);
    }

    [Fact]
    public async Task when_201_jobs_complete_then_per_job_progress_fires_at_100_200_and_the_final_job()
    {
        _ = await RunAsync(SucceedingJobs(201));

        PerJobProgressCompletedCounts().ShouldBe([100, 200, 201]);
    }

    [Fact]
    public async Task when_the_final_job_is_not_on_a_100_boundary_then_its_per_job_progress_event_still_fires_with_the_syncing_state()
    {
        _ = await RunAsync(SucceedingJobs(999));

        var last = perJobProgress.Last();
        last.Completed.ShouldBe(999);
        last.SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public async Task when_exactly_100_jobs_complete_then_only_one_per_job_progress_event_fires()
    {
        _ = await RunAsync(SucceedingJobs(100));

        PerJobProgressCompletedCounts().ShouldBe([100]);
    }

    [Fact]
    public async Task when_99_jobs_complete_then_on_job_completed_fires_for_every_job_regardless_of_the_throttle()
    {
        _ = await RunAsync(SucceedingJobs(99));

        completedJobs.Count.ShouldBe(99);
    }

    [Fact]
    public async Task when_all_jobs_succeed_then_the_failed_count_is_zero()
    {
        int failedCount = await RunAsync(SucceedingJobs(2));

        failedCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_one_job_fails_then_the_failed_count_is_one()
    {
        int failedCount = await RunAsync($"{FailedJobPrefix}file.txt");

        failedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_two_jobs_fail_then_the_failed_count_is_two()
    {
        int failedCount = await RunAsync($"{FailedJobPrefix}1.txt", $"{FailedJobPrefix}2.txt");

        failedCount.ShouldBe(2);
    }

    private sealed class ScriptedSyncWorker : ISyncWorker
    {
        public async Task RunAsync(ChannelReader<SyncJob> reader, string accountId, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncJob, bool, string?, Task> onJobComplete, CancellationToken cancellationToken)
        {
            await foreach (var job in reader.ReadAllAsync(cancellationToken))
            {
                bool fails = job.Target.RelativePath.StartsWith(FailedJobPrefix, StringComparison.Ordinal);
                await onJobComplete(job, !fails, fails ? "scripted failure" : null);
            }
        }
    }
}
