using System.Threading.Channels;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

public sealed class GivenASyncWorkerFailureLogging
{
    private const string AccountId = "account-001";

    private readonly IJobHandler handler = Substitute.For<IJobHandler>();
    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();

    private static DownloadSyncJob MakeDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId("item-abc"));
        var target = SyncFileTargetFactory.Create("/tmp/file.txt", "Desktop/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    [Fact]
    public async Task when_a_job_handler_throws_then_an_error_is_logged_with_event_id_2005_and_the_job_is_reported_failed()
    {
        var job = MakeDownloadJob();
        var logger = new TestLogger<SyncWorker>();
        handler.CanHandle(job).Returns(true);
        handler.HandleAsync(job, AccountId, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncJob, string>>>(_ => throw new IOException("disk full"));
        var channel = Channel.CreateUnbounded<SyncJob>();
        channel.Writer.TryWrite(job);
        channel.Writer.Complete();
        bool? reportedSuccess = null;

        await new SyncWorker(1, [handler], syncRepository, logger).RunAsync(channel.Reader, AccountId, _ => Task.FromResult("token"), (_, success, _) =>
        {
            reportedSuccess = success;

            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.EventId.Id == 2005);
        reportedSuccess.ShouldBe(false);
    }
}
