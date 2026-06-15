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
public sealed class SyncJobExecutor(ISyncRepository syncRepository, ISyncedItemRepository syncedItemRepository, ISyncPipeline syncPipeline, IFileClassificationRepository classificationRepository, IFileSystem fileSystem, ISettingsService settingsService, IFileAutoCategorisor fileAutoCategorisor) : ISyncJobExecutor
{
    /// <inheritdoc />
    public async Task ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IAsyncEnumerable<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, CancellationToken ct)
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
            return;
        }

        var successfulJobs = new ConcurrentBag<SyncJob>();
        var firstJob = enumerator.Current;

        await syncPipeline.RunAsync(
            EnqueueAndYield(firstJob, enumerator, ct),
            tokenFactory,
            onProgress,
            args =>
            {
                if (args.Job.Status.State == SyncJobState.Completed)
                    successfulJobs.Add(args.Job);
                onJobCompleted(args);
            },
            account.Id.Id,
            string.Empty,
            settingsService.Current.ConcurrentWorkerCount,
            ct).ConfigureAwait(false);

        if (successfulJobs.IsEmpty)
            return;

        var mappings = await classificationRepository.GetAllKeywordMappingsAsync(ct).ConfigureAwait(false);

        foreach (var job in successfulJobs)
        {
            string remotePath = NormaliseRemotePath(job.Target.RelativePath);

            if (job is DownloadSyncJob)
            {
                var entity = SyncedItemEntityFactory.CreateFromDownloadJob(account.Id, job, remotePath);
                int syncedItemId = await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
                syncedItems[job.Remote.RemoteItemId.Id] = entity;
                await ClassifyAsync(syncedItemId, remotePath, mappings, ct).ConfigureAwait(false);
            }
            else if (job is UploadSyncJob uploadJob && uploadJob.UploadedRemoteItemId is Option<string>.Some uploadedId)
            {
                var entity = SyncedItemEntityFactory.CreateFromUploadJob(account.Id, uploadJob, uploadedId.Value, remotePath, fileSystem);
                int syncedItemId = await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
                syncedItems[uploadedId.Value] = entity;
                await ClassifyAsync(syncedItemId, remotePath, mappings, ct).ConfigureAwait(false);
            }
        }
    }

    private async IAsyncEnumerable<SyncJob> EnqueueAndYield(SyncJob first, IAsyncEnumerator<SyncJob> rest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        try
        {
            await syncRepository.EnqueueJobAsync(first, ct).ConfigureAwait(false);
            yield return first;

            while (await rest.MoveNextAsync().ConfigureAwait(false))
            {
                await syncRepository.EnqueueJobAsync(rest.Current, ct).ConfigureAwait(false);
                yield return rest.Current;
            }
        }
        finally
        {
            await rest.DisposeAsync().ConfigureAwait(false);
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
