using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncWorkerFactory
{
    private readonly IJobHandler _handler = Substitute.For<IJobHandler>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();

    private SyncWorkerFactory CreateSut() => new([_handler], _syncRepository, Substitute.For<ILogger<SyncWorker>>());

    [Fact]
    public void when_create_is_called_then_returns_non_null_worker()
    {
        var worker = CreateSut().Create(1);

        worker.ShouldNotBeNull();
    }

    [Fact]
    public void when_create_is_called_then_returns_sync_worker_instance()
    {
        var worker = CreateSut().Create(1);

        worker.ShouldBeOfType<SyncWorker>();
    }

    [Fact]
    public void when_create_is_called_with_different_ids_then_returns_distinct_instances()
    {
        var sut = CreateSut();

        var worker1 = sut.Create(1);
        var worker2 = sut.Create(2);

        worker1.ShouldNotBeSameAs(worker2);
    }
}
