using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncProgressTracker
{
    private const string AccountIdValue = "account-1";
    private const string FolderIdValue  = "folder-1";
    private const int TestProgressInterval = 100;

    private static DownloadSyncJob MakeDownloadJob(string relativePath = "folder/file.txt")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("/tmp/test-file.txt", relativePath);
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private static SyncProgressTracker CreateTracker(int total)
    {
        var tracker = new SyncProgressTracker(AccountIdValue, FolderIdValue, TestProgressInterval);
        tracker.SetTotal(total);

        return tracker;
    }

    private static void CompleteJobs(SyncProgressTracker sut, int count, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs>? onJobCompleted = null)
    {
        for (var i = 0; i < count; i++)
            sut.RecordCompletion(MakeDownloadJob($"folder/file{i}.txt"), true, null, onProgress, onJobCompleted ?? (_ => { }));
    }

    [Fact]
    public void when_single_job_completes_then_sync_state_is_syncing()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(1);

        sut.RecordCompletion(MakeDownloadJob(), true, null, progressEvents.Add, _ => { });

        progressEvents[0].SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_job_succeeds_then_on_job_completed_receives_completed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateTracker(1);

        sut.RecordCompletion(MakeDownloadJob(), true, null, _ => { }, completedEvents.Add);

        completedEvents[0].Job.Status.State.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public void when_job_fails_then_on_job_completed_receives_failed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateTracker(1);

        sut.RecordCompletion(MakeDownloadJob(), false, "download error", _ => { }, completedEvents.Add);

        completedEvents[0].Job.Status.State.ShouldBe(SyncJobState.Failed);
    }

    [Fact]
    public void when_single_job_completes_then_on_progress_is_called_once()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(1);

        sut.RecordCompletion(MakeDownloadJob(), true, null, progressEvents.Add, _ => { });

        progressEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void when_job_completes_then_on_job_completed_is_called_once()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = CreateTracker(1);

        sut.RecordCompletion(MakeDownloadJob(), true, null, _ => { }, completedEvents.Add);

        completedEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void when_last_of_two_jobs_completes_then_sync_state_is_syncing()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(2);

        sut.RecordCompletion(MakeDownloadJob("a/1.txt"), true, null, progressEvents.Add, _ => { });
        sut.RecordCompletion(MakeDownloadJob("a/2.txt"), true, null, progressEvents.Add, _ => { });

        progressEvents.Last().SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_99_of_1000_jobs_complete_then_no_progress_event_fires()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(1000);

        CompleteJobs(sut, 99, progressEvents.Add);

        progressEvents.ShouldBeEmpty();
    }

    [Fact]
    public void when_100th_of_1000_jobs_completes_then_exactly_one_progress_event_fires()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(1000);

        CompleteJobs(sut, 100, progressEvents.Add);

        progressEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void when_100th_of_1000_jobs_completes_then_sync_state_is_syncing()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(1000);

        CompleteJobs(sut, 100, progressEvents.Add);

        progressEvents[0].SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_201_jobs_complete_then_progress_fires_at_100_200_and_final()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(201);

        CompleteJobs(sut, 201, progressEvents.Add);

        progressEvents.Count.ShouldBe(3);
    }

    [Fact]
    public void when_final_job_completes_at_non_100_boundary_then_progress_still_fires()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(999);

        CompleteJobs(sut, 999, progressEvents.Add);

        progressEvents.Last().SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_total_is_exactly_100_then_only_one_progress_event_fires()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateTracker(100);

        CompleteJobs(sut, 100, progressEvents.Add);

        progressEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void when_on_job_completed_fires_for_every_job_regardless_of_throttle()
    {
        var jobCompletedCount = 0;
        var sut = CreateTracker(1000);

        CompleteJobs(sut, 99, _ => { }, _ => jobCompletedCount++);

        jobCompletedCount.ShouldBe(99);
    }
}
