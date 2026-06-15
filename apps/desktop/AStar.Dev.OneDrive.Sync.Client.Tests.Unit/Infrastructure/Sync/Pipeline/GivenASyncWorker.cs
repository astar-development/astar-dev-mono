using System.Threading.Channels;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncWorker
{
    private const string AccountId = "account-001";
    private const string AccessToken = "test-token";
    private const string ItemId = "item-abc";

    private readonly IJobHandler _handler = Substitute.For<IJobHandler>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();

    private SyncWorker CreateSut() => new(1, [_handler], _syncRepository, Substitute.For<ILogger<SyncWorker>>());

    private static DownloadSyncJob MakeDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private static async Task<(List<SyncJob> Completed, List<string?> Errors)> RunWorkerWithJobsAsync(SyncWorker worker, IEnumerable<SyncJob> jobs, CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<SyncJob>();
        var completed = new List<SyncJob>();
        var errors = new List<string?>();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult(AccessToken);

        foreach(var job in jobs)
            channel.Writer.TryWrite(job);

        channel.Writer.Complete();

        await worker.RunAsync(channel.Reader, AccountId, tokenFactory, (job, _, error) =>
        {
            completed.Add(job);
            errors.Add(error);

            return Task.CompletedTask;
        }, ct);

        return (completed, errors);
    }

    [Fact]
    public async Task when_handler_can_handle_job_then_handler_handle_async_is_called()
    {
        var job = MakeDownloadJob();
        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncJob, string>.Ok(job));

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _handler.Received(1).HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_handler_can_handle_job_then_job_completes_with_error()
    {
        var job = MakeDownloadJob();
        _handler.CanHandle(job).Returns(false);

        var (_, errors) = await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        errors.Count.ShouldBe(1);
        errors[0].ShouldNotBeNull();
    }

    [Fact]
    public async Task when_handler_succeeds_then_job_state_set_to_completed()
    {
        var job = MakeDownloadJob();
        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncJob, string>.Ok(job));

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).UpdateJobStateAsync(job.Status.Id, SyncJobState.Completed, Option.None<string>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_handler_fails_then_job_state_set_to_failed()
    {
        var job = MakeDownloadJob();
        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncJob, string>.Error("handler error"));

        await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, Arg.Any<Option<string>>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_cancellation_requested_then_job_state_reset_to_queued()
    {
        var job = MakeDownloadJob();
        using var cts = new CancellationTokenSource();

        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncJob, string>>>(async _ =>
            {
                await cts.CancelAsync();
                throw new OperationCanceledException();
            });

        try
        {
            await RunWorkerWithJobsAsync(CreateSut(), [job], cts.Token);
        }
        catch(OperationCanceledException) { }

        await _syncRepository.Received(1).UpdateJobStateAsync(job.Status.Id, SyncJobState.Queued, Option.None<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task when_worker_runs_job_then_token_factory_is_passed_to_handler()
    {
        var job = MakeDownloadJob();
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("fresh-token");
        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncJob, string>.Ok(job));

        var channel = Channel.CreateUnbounded<SyncJob>();
        channel.Writer.TryWrite(job);
        channel.Writer.Complete();

        await CreateSut().RunAsync(channel.Reader, AccountId, tokenFactory, (_, _, _) => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _handler.Received(1).HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_handler_throws_sync_re_auth_required_exception_then_job_state_set_to_queued()
    {
        var job = MakeDownloadJob();
        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncJob, string>>>(_ => throw new SyncReAuthRequiredException());

        try
        {
            await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);
        }
        catch(SyncReAuthRequiredException) { }

        await _syncRepository.Received(1).UpdateJobStateAsync(job.Status.Id, SyncJobState.Queued, Option.None<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task when_handler_throws_sync_re_auth_required_exception_then_exception_is_rethrown()
    {
        var job = MakeDownloadJob();
        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncJob, string>>>(_ => throw new SyncReAuthRequiredException());

        var act = async () => await RunWorkerWithJobsAsync(CreateSut(), [job], TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<SyncReAuthRequiredException>();
    }
}
