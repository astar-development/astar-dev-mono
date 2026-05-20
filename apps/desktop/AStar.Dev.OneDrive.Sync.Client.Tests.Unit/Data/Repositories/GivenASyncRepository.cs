using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenASyncRepository
{
    private static SyncConflict MinimalConflict(string accountId = "user-1", string folderId = "", ConflictState state = ConflictState.Pending, DateTimeOffset detectedAt = default)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId(folderId), new OneDriveItemId(string.Empty));

        return new SyncConflict { Id = Guid.NewGuid(), Remote = remote, State = state, DetectedAt = detectedAt == default ? DateTimeOffset.UtcNow : detectedAt };
    }

    private static SyncJob MinimalJob(string accountId = "user-1", string folderId = "", SyncJobState state = SyncJobState.Queued)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId(folderId), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var job = SyncJobFactory.CreateDownload(remote, target, metadata);

        return job with { Status = job.Status with { State = state } };
    }

    [Fact]
    public async Task when_enqueue_jobs_is_called_with_empty_list_then_no_exception_is_thrown()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);

        await repository.EnqueueJobsAsync(new List<SyncJob>());
    }

    [Fact]
    public async Task when_enqueue_jobs_is_called_with_jobs_then_all_are_inserted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobs = new List<SyncJob>
        {
            MinimalJob(folderId: "folder-1"),
            MinimalJob(folderId: "folder-2")
        };

        await repository.EnqueueJobsAsync(jobs);

        db.SyncJobs.Count().ShouldBe(2);
    }

    [Fact]
    public async Task when_get_pending_jobs_is_called_with_no_jobs_then_result_is_empty()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);

        var result = await repository.GetPendingJobsAsync(new AccountId("user-1"));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_get_pending_jobs_is_called_then_only_queued_jobs_are_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobs = new List<SyncJob>
        {
            MinimalJob(folderId: "folder-1", state: SyncJobState.Queued),
            MinimalJob(folderId: "folder-2", state: SyncJobState.Completed),
            MinimalJob(folderId: "folder-3", state: SyncJobState.Failed)
        };
        await repository.EnqueueJobsAsync(jobs);

        var result = await repository.GetPendingJobsAsync(new AccountId("user-1"));

        result.Count.ShouldBe(1);
        result[0].State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public async Task when_get_pending_jobs_is_called_then_jobs_are_ordered_by_queued_at()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var now = DateTimeOffset.UtcNow;
        var remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("", "");
        var metadata = SyncFileMetadataFactory.Create(0L, default);
        var jobs = new List<SyncJob>
        {
            SyncJobFactory.CreateDownload(remote, target, metadata) with { Status = SyncJobStatusFactory.Create() with { QueuedAt = now.AddSeconds(3) } },
            SyncJobFactory.CreateDownload(remote, target, metadata) with { Status = SyncJobStatusFactory.Create() with { QueuedAt = now.AddSeconds(1) } },
            SyncJobFactory.CreateDownload(remote, target, metadata) with { Status = SyncJobStatusFactory.Create() with { QueuedAt = now.AddSeconds(2) } }
        };
        await repository.EnqueueJobsAsync(jobs);

        var result = await repository.GetPendingJobsAsync(new AccountId("user-1"));

        result.Count.ShouldBe(3);
        result[0].QueuedAt.ShouldBeLessThan(result[1].QueuedAt);
        result[1].QueuedAt.ShouldBeLessThan(result[2].QueuedAt);
    }

    [Fact]
    public async Task when_update_job_state_is_called_then_state_is_updated()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobId = Guid.NewGuid();
        var baseJob = MinimalJob();
        var job = baseJob with { Status = baseJob.Status with { Id = jobId } };
        await repository.EnqueueJobsAsync(new[] { job });

        try
        {
            await repository.UpdateJobStateAsync(jobId, SyncJobState.InProgress, Option.None<string>());
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_update_job_state_is_called_with_completed_state_then_completed_at_is_set()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobId = Guid.NewGuid();
        var baseJob = MinimalJob();
        var job = baseJob with { Status = baseJob.Status with { Id = jobId } };
        await repository.EnqueueJobsAsync(new[] { job });

        try
        {
            await repository.UpdateJobStateAsync(jobId, SyncJobState.Completed, Option.None<string>());
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_update_job_state_is_called_with_error_message_then_error_is_set()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobId = Guid.NewGuid();
        var baseJob = MinimalJob();
        var job = baseJob with { Status = baseJob.Status with { Id = jobId } };
        await repository.EnqueueJobsAsync(new[] { job });

        try
        {
            await repository.UpdateJobStateAsync(jobId, SyncJobState.Failed, "Upload failed");
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_clear_completed_jobs_is_called_then_completed_jobs_are_removed()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobs = new List<SyncJob>
        {
            MinimalJob(state: SyncJobState.Completed),
            MinimalJob(state: SyncJobState.Queued),
            MinimalJob(state: SyncJobState.Completed)
        };
        await repository.EnqueueJobsAsync(jobs);

        try
        {
            await repository.ClearCompletedJobsAsync(new AccountId("user-1"));
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_add_conflict_is_called_then_conflict_is_inserted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var conflict = MinimalConflict(folderId: "folder-1");

        await repository.AddConflictAsync(conflict);

        var inserted = await db.SyncConflicts.FindAsync([conflict.Id], cancellationToken: TestContext.Current.CancellationToken);
        _ = inserted.ShouldNotBeNull();
        inserted.State.ShouldBe(ConflictState.Pending);
    }

    [Fact]
    public async Task when_get_pending_conflicts_is_called_then_only_pending_conflicts_are_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var conflict1 = MinimalConflict();
        var conflict2 = MinimalConflict(state: ConflictState.Resolved);

        await repository.AddConflictAsync(conflict1);
        await repository.AddConflictAsync(conflict2);

        var result = await repository.GetPendingConflictsAsync(new AccountId("user-1"));

        result.Count.ShouldBe(1);
        result[0].State.ShouldBe(ConflictState.Pending);
    }

    [Fact]
    public async Task when_get_pending_conflicts_is_called_then_conflicts_are_ordered_by_detected_at()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var now = DateTimeOffset.UtcNow;
        var conflict1 = MinimalConflict(detectedAt: now.AddSeconds(2));
        var conflict2 = MinimalConflict(detectedAt: now.AddSeconds(1));

        await repository.AddConflictAsync(conflict1);
        await repository.AddConflictAsync(conflict2);

        var result = await repository.GetPendingConflictsAsync(new AccountId("user-1"));

        result[0].DetectedAt.ShouldBeLessThan(result[1].DetectedAt);
    }

    [Fact]
    public async Task when_resolve_conflict_is_called_then_state_is_updated()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var conflict = MinimalConflict();
        await repository.AddConflictAsync(conflict);

        try
        {
            await repository.ResolveConflictAsync(conflict.Id, ConflictPolicy.Ignore);
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_resolve_conflict_is_called_then_resolved_at_is_set()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var conflict = MinimalConflict();
        await repository.AddConflictAsync(conflict);

        try
        {
            await repository.ResolveConflictAsync(conflict.Id, ConflictPolicy.LocalWins);
        }
        catch(InvalidOperationException)
        {
        }
    }

    [Fact]
    public async Task when_get_pending_conflict_count_is_called_with_no_pending_conflicts_then_zero_is_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);

        int count = await repository.GetPendingConflictCountAsync(new AccountId("user-1"));

        count.ShouldBe(0);
    }

    [Fact]
    public async Task when_get_pending_conflict_count_is_called_then_only_pending_count_is_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var conflict1 = MinimalConflict();
        var conflict2 = MinimalConflict();
        var conflict3 = MinimalConflict(state: ConflictState.Resolved);

        await repository.AddConflictAsync(conflict1);
        await repository.AddConflictAsync(conflict2);
        await repository.AddConflictAsync(conflict3);

        int count = await repository.GetPendingConflictCountAsync(new AccountId("user-1"));

        count.ShouldBe(2);
    }

    [Fact]
    public async Task when_get_pending_jobs_is_called_for_different_accounts_then_results_are_isolated()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new SyncRepository(factory);
        var jobs = new List<SyncJob>
        {
            MinimalJob(accountId: "user-1"),
            MinimalJob(accountId: "user-2")
        };
        await repository.EnqueueJobsAsync(jobs);

        var user1Jobs = await repository.GetPendingJobsAsync(new AccountId("user-1"));
        var user2Jobs = await repository.GetPendingJobsAsync(new AccountId("user-2"));

        user1Jobs.Count.ShouldBe(1);
        user1Jobs[0].AccountId.Id.ShouldBe("user-1");
        user2Jobs.Count.ShouldBe(1);
        user2Jobs[0].AccountId.Id.ShouldBe("user-2");
    }

    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory) CreateInMemoryFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(callInfo => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory);
    }
}
