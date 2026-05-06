using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncEventAggregator
{
    private readonly ISyncService _syncService = Substitute.For<ISyncService>();
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly IUiDispatcher _dispatcher = new InlineUiDispatcher();

    private SyncEventAggregator CreateSut() => new(_syncService, _scheduler, _dispatcher);

    [Fact]
    public void when_sync_service_raises_progress_then_aggregator_raises_progress()
    {
        var sut = CreateSut();
        SyncProgressEventArgs? captured = null;
        sut.SyncProgressChanged += (_, args) => captured = args;
        var eventArgs = new SyncProgressEventArgs("acc-1", "folder-1", 1, 10, "file.txt", SyncState.Syncing);

        _syncService.SyncProgressChanged += Raise.EventWith(eventArgs);

        captured.ShouldNotBeNull();
        captured.AccountId.ShouldBe("acc-1");
    }

    [Fact]
    public void when_sync_service_raises_job_completed_then_aggregator_raises_job_completed()
    {
        var sut = CreateSut();
        JobCompletedEventArgs? captured = null;
        sut.JobCompleted += (_, args) => captured = args;

        var remote = RemoteItemRefFactory.Create(new AccountId("acc-2"), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);
        var eventArgs = new JobCompletedEventArgs(job);

        _syncService.JobCompleted += Raise.EventWith(eventArgs);

        captured.ShouldNotBeNull();
        captured.Job.Remote.AccountId.Id.ShouldBe("acc-2");
    }

    [Fact]
    public void when_sync_service_raises_conflict_then_aggregator_raises_conflict()
    {
        var sut = CreateSut();
        SyncConflict? captured = null;
        sut.ConflictDetected += (_, conflict) => captured = conflict;
        var conflict = new SyncConflict { Remote = RemoteItemRefFactory.Create(new AccountId("acc-3"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)) };

        _syncService.ConflictDetected += Raise.Event<EventHandler<SyncConflict>>(this, conflict);

        captured.ShouldNotBeNull();
        captured.Remote.AccountId.Id.ShouldBe("acc-3");
    }

    [Fact]
    public void when_scheduler_raises_sync_completed_then_aggregator_raises_sync_completed()
    {
        var sut = CreateSut();
        string? captured = null;
        sut.SyncCompleted += (_, accountId) => captured = accountId;

        _scheduler.SyncCompleted += Raise.Event<EventHandler<string>>(this, "acc-4");

        captured.ShouldNotBeNull();
        captured.ShouldBe("acc-4");
    }
}
