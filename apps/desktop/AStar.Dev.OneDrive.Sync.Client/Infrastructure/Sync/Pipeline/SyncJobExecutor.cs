using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <inheritdoc />
public sealed class SyncJobExecutor(ISyncRepository syncRepository, ISyncedItemRepository syncedItemRepository, ISyncPipeline syncPipeline, IFileSystem fileSystem, ISettingsService settingsService, IFileAutoCategorisor fileAutoCategorisor, ICategoryResolutionService categoryResolutionService, IFileClassificationRepository fileClassificationRepository) : ISyncJobExecutor
{
    private const int EnqueueBatchSize = 100;

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IAsyncEnumerable<SyncJob> jobs, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, Action<SyncProgressEventArgs> onProgress, Func<JobCompletedEventArgs, Task> onJobCompleted, CancellationToken ct)
    {
        var enumerator = jobs.GetAsyncEnumerator(ct);
        bool hasFirst;
        try
        {
            hasFirst = await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
        catch
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        if (!hasFirst)
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            return 0;
        }

        var mappings = await fileClassificationRepository.GetAllCategoriesAsync(ct).ConfigureAwait(false);
        var firstJob = enumerator.Current;

        return await syncPipeline.RunAsync(
            EnqueueAndYield(firstJob, enumerator, ct),
            tokenFactory,
            onProgress,
            async args =>
            {
                if (args.Job.Status.State == SyncJobState.Completed)
                {
                    string remotePath = NormaliseRemotePath(args.Job.Target.RelativePath);

                    if (args.Job is DownloadSyncJob)
                    {
                        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(account.Id, args.Job, remotePath);
                        await UpsertWithClassificationsAsync(entity, remotePath, mappings, ct).ConfigureAwait(false);
                        syncedItems[args.Job.Remote.RemoteItemId.Id] = entity;
                    }
                    else if (args.Job is UploadSyncJob uploadJob && uploadJob.UploadedRemoteItemId is Option<string>.Some uploadedId)
                    {
                        var entity = SyncedItemEntityFactory.CreateFromUploadJob(account.Id, uploadJob, uploadedId.Value, remotePath, fileSystem);
                        await UpsertWithClassificationsAsync(entity, remotePath, mappings, ct).ConfigureAwait(false);
                        syncedItems[uploadedId.Value] = entity;
                    }
                }

                await onJobCompleted(args).ConfigureAwait(false);
            },
            account.Id.Id,
            string.Empty,
            settingsService.Current.ConcurrentWorkerCount,
            ct).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<SyncJob> EnqueueAndYield(SyncJob first, IAsyncEnumerator<SyncJob> rest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var batch = new List<SyncJob>(EnqueueBatchSize) { first };

        try
        {
            while (await rest.MoveNextAsync().ConfigureAwait(false))
            {
                batch.Add(rest.Current);

                if (batch.Count >= EnqueueBatchSize)
                {
                    await syncRepository.EnqueueJobsAsync(batch, ct).ConfigureAwait(false);
                    foreach (var job in batch)
                        yield return job;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await syncRepository.EnqueueJobsAsync(batch, ct).ConfigureAwait(false);
                foreach (var job in batch)
                    yield return job;
            }
        }
        finally
        {
            await rest.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task UpsertWithClassificationsAsync(SyncedItemEntity entity, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct)
    {
        var analyserResult = fileAutoCategorisor.Categorise(remotePath);
        var classifications = ClassificationCombiner.Combine(FileClassifier.Classify(remotePath, mappings), analyserResult.Match(c => (IReadOnlyList<FileClassification>)[c], () => []));
        var categoryIds = await categoryResolutionService.ResolveManyAsync(classifications, ct).ConfigureAwait(false);
        await syncedItemRepository.UpsertWithClassificationsAsync(entity, categoryIds, ct).ConfigureAwait(false);
    }

    private static string NormaliseRemotePath(string? relativePath)
        => string.IsNullOrEmpty(relativePath) ? "/" : $"/{relativePath.TrimStart('/')}";
}
