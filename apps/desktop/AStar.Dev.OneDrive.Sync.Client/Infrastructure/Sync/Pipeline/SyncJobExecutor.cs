using System.Collections.Concurrent;
using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <inheritdoc />
public sealed class SyncJobExecutor(ISyncRepository syncRepository, ISyncedItemRepository syncedItemRepository, ISyncPipeline syncPipeline, IFileClassificationRepository classificationRepository, IFileSystem fileSystem, ISettingsService settingsService, IFileAutoCategorisor fileAutoCategorisor) : ISyncJobExecutor
{
    /// <inheritdoc />
    public async Task ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IReadOnlyList<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, CancellationToken ct)
    {
        if(jobs.Count == 0)
            return;

        await syncRepository.EnqueueJobsAsync(jobs).ConfigureAwait(false);

        var successfulJobs = new ConcurrentBag<SyncJob>();

        await syncPipeline.RunAsync(
            jobs,
            tokenFactory,
            onProgress,
            args =>
            {
                if(args.Job.Status.State == SyncJobState.Completed)
                    successfulJobs.Add(args.Job);

                onJobCompleted(args);
            },
            account.Id.Id,
            string.Empty,
            settingsService.Current.ConcurrentWorkerCount,
            ct).ConfigureAwait(false);

        if(successfulJobs.IsEmpty)
            return;

        var mappings = await classificationRepository.GetAllKeywordMappingsAsync(ct).ConfigureAwait(false);

        foreach(var job in successfulJobs)
        {
            string remotePath = NormaliseRemotePath(job.Target.RelativePath);

            if(job is DownloadSyncJob)
            {
                var entity = SyncedItemEntityFactory.CreateFromDownloadJob(account.Id, job, remotePath);
                int syncedItemId = await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
                syncedItems[job.Remote.RemoteItemId.Id] = entity;
                await ClassifyAsync(syncedItemId, remotePath, mappings, ct).ConfigureAwait(false);
            }
            else if(job is UploadSyncJob uploadJob && uploadJob.UploadedRemoteItemId is Option<string>.Some uploadedId)
            {
                var entity = SyncedItemEntityFactory.CreateFromUploadJob(account.Id, uploadJob, remotePath, fileSystem);
                int syncedItemId = await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
                syncedItems[uploadedId.Value] = entity;
                await ClassifyAsync(syncedItemId, remotePath, mappings, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ClassifyAsync(int syncedItemId, string remotePath, IReadOnlyList<KeywordMapping> mappings, CancellationToken ct)
    {
        var analyserResult = fileAutoCategorisor.Categorise(remotePath);
        var classifications = ClassificationCombiner.Combine(FileClassifier.Classify(remotePath, mappings), analyserResult.Match(c => (IReadOnlyList<FileClassification>)[c], () => []));
        await syncedItemRepository.UpsertClassificationsAsync(syncedItemId, classifications, ct).ConfigureAwait(false);
    }

    private static string NormaliseRemotePath(string? relativePath)
        => string.IsNullOrEmpty(relativePath) ? "/" : $"/{relativePath.TrimStart('/')}";
}
