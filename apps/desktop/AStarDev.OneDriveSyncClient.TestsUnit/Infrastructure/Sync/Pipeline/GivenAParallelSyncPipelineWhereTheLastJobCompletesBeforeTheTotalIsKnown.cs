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

public sealed class GivenAParallelSyncPipelineWhereTheLastJobCompletesBeforeTheTotalIsKnown
{
    private const string AccountIdValue = "account-1";
    private const string FolderIdValue = "folder-1";

    private readonly TaskCompletionSource jobCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static ParallelSyncPipeline CreateSut()
    {
        var workerFactory = Substitute.For<ISyncWorkerFactory>();
        workerFactory.Create(Arg.Any<int>()).Returns(_ => new SucceedingDownloadWorker());

        return new ParallelSyncPipeline(workerFactory, Substitute.For<ISyncRepository>(), Substitute.For<ILogger<ParallelSyncPipeline>>(), Options.Create(new SyncSettings { ProgressReportInterval = 100 }));
    }

    private static DownloadSyncJob MakeDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId(""), new OneDriveFolderId(""), new OneDriveItemId(""));
        var target = SyncFileTargetFactory.Create("/tmp/test-file.txt", "folder/file.txt");
        var metadata = SyncFileMetadataFactory.Create(0L, DateTimeOffset.UtcNow);

        return SyncJobFactory.CreateDownload(remote, target, metadata, "https://example.com/file");
    }

    private async IAsyncEnumerable<SyncJob> JobStreamEndingAfterTheJobCompletes()
    {
        yield return MakeDownloadJob();

        await jobCompleted.Task;
    }

    [Fact]
    public async Task when_the_only_job_completes_before_the_stream_ends_then_the_final_per_job_progress_event_is_still_raised()
    {
        var perJobProgressEvents = new List<SyncProgressEventArgs>();
        var sut = CreateSut();

        await sut.RunAsync(
            JobStreamEndingAfterTheJobCompletes(),
            _ => Task.FromResult("test-token"),
            args =>
            {
                if (args.CurrentFile != string.Empty)
                    perJobProgressEvents.Add(args);
            },
            _ =>
            {
                jobCompleted.TrySetResult();

                return Task.CompletedTask;
            },
            AccountIdValue,
            FolderIdValue,
            cancellationToken: TestContext.Current.CancellationToken);

        perJobProgressEvents.Count.ShouldBe(1);
    }

    private sealed class SucceedingDownloadWorker : ISyncWorker
    {
        public async Task RunAsync(ChannelReader<SyncJob> reader, string accountId, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncJob, bool, string?, Task> onJobComplete, CancellationToken cancellationToken)
        {
            await foreach (var job in reader.ReadAllAsync(cancellationToken))
                await onJobComplete(job, true, null);
        }
    }
}
