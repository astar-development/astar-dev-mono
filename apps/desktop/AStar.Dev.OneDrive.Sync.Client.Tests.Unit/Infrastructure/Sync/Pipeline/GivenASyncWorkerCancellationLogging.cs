using System.Threading.Channels;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncWorkerCancellationLogging
{
    private const string AccountId = "account-001";
    private const string AccessToken = "test-token";
    private const string ItemId = "item-abc";

    private readonly IJobHandler _handler = Substitute.For<IJobHandler>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();

    private SyncWorker CreateSut(TestLogger<SyncWorker> logger) => new(1, [_handler], _syncRepository, logger);

    private static DownloadSyncJob MakeDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(ItemId));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    [Fact]
    public async Task when_cancellation_requested_then_warning_logged_with_event_id_2007()
    {
        var job = MakeDownloadJob();
        using var cts = new CancellationTokenSource();
        var logger = new TestLogger<SyncWorker>();

        _handler.CanHandle(job).Returns(true);
        _handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncJob, string>>>(async _ =>
            {
                await cts.CancelAsync();
                throw new OperationCanceledException();
            });

        var channel = Channel.CreateUnbounded<SyncJob>();
        channel.Writer.TryWrite(job);
        channel.Writer.Complete();

        try
        {
            await CreateSut(logger).RunAsync(channel.Reader, AccountId, _ => Task.FromResult(AccessToken), (_, _, _) => { }, cts.Token);
        }
        catch (OperationCanceledException) { }

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.EventId.Id == 2007);
    }
}
