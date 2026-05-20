using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
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

    private static DownloadSyncJob MakeDownloadJob(string relativePath = "folder/file.txt")
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("/tmp/test-file.txt", relativePath);
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    [Fact]
    public void when_first_of_two_jobs_completes_then_sync_state_is_syncing()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = new SyncProgressTracker(2, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob(), true, null, progressEvents.Add, _ => { });

        progressEvents[0].SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_last_of_two_jobs_completes_then_sync_state_is_idle()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = new SyncProgressTracker(2, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob("a/1.txt"), true, null, progressEvents.Add, _ => { });
        sut.RecordCompletion(MakeDownloadJob("a/2.txt"), true, null, progressEvents.Add, _ => { });

        progressEvents[1].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public void when_single_job_completes_then_sync_state_is_idle()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = new SyncProgressTracker(1, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob(), true, null, progressEvents.Add, _ => { });

        progressEvents[0].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public void when_job_succeeds_then_on_job_completed_receives_completed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = new SyncProgressTracker(1, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob(), true, null, _ => { }, completedEvents.Add);

        completedEvents[0].Job.Status.State.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public void when_job_fails_then_on_job_completed_receives_failed_state()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = new SyncProgressTracker(1, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob(), false, "download error", _ => { }, completedEvents.Add);

        completedEvents[0].Job.Status.State.ShouldBe(SyncJobState.Failed);
    }

    [Fact]
    public void when_job_completes_then_on_progress_is_called_once()
    {
        var progressEvents = new List<SyncProgressEventArgs>();
        var sut = new SyncProgressTracker(1, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob(), true, null, progressEvents.Add, _ => { });

        progressEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void when_job_completes_then_on_job_completed_is_called_once()
    {
        var completedEvents = new List<JobCompletedEventArgs>();
        var sut = new SyncProgressTracker(1, AccountIdValue, FolderIdValue);

        sut.RecordCompletion(MakeDownloadJob(), true, null, _ => { }, completedEvents.Add);

        completedEvents.Count.ShouldBe(1);
    }
}
