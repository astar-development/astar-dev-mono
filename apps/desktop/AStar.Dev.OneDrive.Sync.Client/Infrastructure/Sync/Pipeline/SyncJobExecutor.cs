using System.Collections.Concurrent;
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
public sealed class SyncJobExecutor(ISyncRepository syncRepository, ISyncPipeline syncPipeline, ISettingsService settingsService, ISyncedItemRegistrar syncedItemRegistrar) : ISyncJobExecutor
{
    private const int EnqueueBatchSize = 100;

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IAsyncEnumerable<SyncJob> jobs, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<FileClassificationCategory> mappings, Action<SyncProgressEventArgs> onProgress, Func<JobCompletedEventArgs, Task> onJobCompleted, CancellationToken ct)
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
                        await syncedItemRegistrar.RegisterDownloadAsync(account.Id, args.Job, remotePath, mappings, syncedItems, ct).ConfigureAwait(false);
                    }
                    else if (args.Job is UploadSyncJob uploadJob && uploadJob.UploadedRemoteItemId is Option<string>.Some uploadedId)
                    {
                        await syncedItemRegistrar.RegisterUploadAsync(account.Id, uploadJob, uploadedId.Value, remotePath, mappings, syncedItems, ct).ConfigureAwait(false);
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

    private static string NormaliseRemotePath(string? relativePath)
        => string.IsNullOrEmpty(relativePath) ? "/" : $"/{relativePath.TrimStart('/')}";
}
