using System.Collections.Concurrent;
using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class SyncJobExecutor(ISyncRepository syncRepository, ISyncedItemRepository syncedItemRepository, IParallelDownloadPipeline parallelDownloadPipeline, IFileSystem fileSystem) : ISyncJobExecutor
{
    /// <inheritdoc />
    public async Task ExecuteAsync(OneDriveAccount account, string accessToken, IReadOnlyList<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, CancellationToken ct)
    {
        if(jobs.Count == 0)
            return;

        await syncRepository.EnqueueJobsAsync(jobs);

        var successfulJobs = new ConcurrentBag<SyncJob>();

        await parallelDownloadPipeline.RunAsync(
            jobs,
            accessToken,
            onProgress,
            args =>
            {
                if(args.Job.State == SyncJobState.Completed)
                    successfulJobs.Add(args.Job);

                onJobCompleted(args);
            },
            account.Id.Id,
            string.Empty,
            ct: ct);

        foreach(var job in successfulJobs)
        {
            var remotePath = NormaliseRemotePath(job.RelativePath);

            if(job.Direction == SyncDirection.Download)
            {
                var entity = SyncedItemEntityFactory.CreateFromDownloadJob(account.Id, job, remotePath);
                await syncedItemRepository.UpsertAsync(entity, ct);
                syncedItems[job.RemoteItemId] = entity;
            }
            else if(job.Direction == SyncDirection.Upload && job.UploadedRemoteItemId is not null)
            {
                var entity = SyncedItemEntityFactory.CreateFromUploadJob(account.Id, job, remotePath, fileSystem);
                await syncedItemRepository.UpsertAsync(entity, ct);
                syncedItems[job.UploadedRemoteItemId] = entity;
            }
        }
    }

    private static string NormaliseRemotePath(string? relativePath)
        => string.IsNullOrEmpty(relativePath) ? "/" : $"/{relativePath.TrimStart('/')}";
}
