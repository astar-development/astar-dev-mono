using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Dashboard.Throughput;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard.Throughput;

public sealed class GivenAThroughputRecorder
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ISyncThroughputRepository _repository = Substitute.For<ISyncThroughputRepository>();
    private readonly ThroughputRecorder _sut;

    public GivenAThroughputRecorder()
    {
        _sut = new ThroughputRecorder(_syncEventAggregator, _repository, new FakeTimeProvider(Now), Substitute.For<ILogger<ThroughputRecorder>>());
        _sut.SubscribeToSyncEvents();
    }

    private static DownloadSyncJob Download(long size, SyncJobState state)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId(""), new OneDriveItemId("item"));
        var job = SyncJobFactory.CreateDownload(remote, SyncFileTargetFactory.Create("", ""), SyncFileMetadataFactory.Create(size, default));

        return job with { Status = job.Status with { State = state } };
    }

    [Fact]
    public async Task when_a_download_completes_then_its_size_is_recorded_for_its_account()
    {
        _syncEventAggregator.JobCompleted += Raise.EventWith(new JobCompletedEventArgs(Download(2048, SyncJobState.Completed)));
        await Task.Delay(200, TestContext.Current.CancellationToken);

        await _repository.Received(1).RecordAsync(new AccountId("acc-1"), 2048, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_job_fails_then_nothing_is_recorded()
    {
        _syncEventAggregator.JobCompleted += Raise.EventWith(new JobCompletedEventArgs(Download(2048, SyncJobState.Failed)));
        await Task.Delay(200, TestContext.Current.CancellationToken);

        await _repository.DidNotReceiveWithAnyArgs().RecordAsync(Arg.Any<AccountId>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_repository_throws_then_the_exception_is_swallowed()
    {
        _repository.RecordAsync(Arg.Any<AccountId>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("boom"));

        _syncEventAggregator.JobCompleted += Raise.EventWith(new JobCompletedEventArgs(Download(1, SyncJobState.Completed)));
        await Task.Delay(200, TestContext.Current.CancellationToken);

        await _repository.Received(1).RecordAsync(Arg.Any<AccountId>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
