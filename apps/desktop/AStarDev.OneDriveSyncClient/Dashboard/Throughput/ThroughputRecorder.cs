using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Dashboard.Throughput;

/// <inheritdoc />
public sealed class ThroughputRecorder(ISyncEventAggregator syncEventAggregator, ISyncThroughputRepository repository, TimeProvider timeProvider, ILogger<ThroughputRecorder> logger) : IThroughputRecorder
{
    /// <inheritdoc />
    public void SubscribeToSyncEvents() => syncEventAggregator.JobCompleted += OnJobCompleted;

    private async void OnJobCompleted(object? sender, JobCompletedEventArgs args)
    {
        if (args.Job.Status.State != SyncJobState.Completed)
            return;

        try
        {
            await repository.RecordAsync(args.Job.Remote.AccountId, args.Job.Metadata.FileSize, timeProvider.GetUtcNow(), CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.ThroughputRecordFailed(logger, args.Job.Remote.AccountId.Value, ex.Message, ex);
        }
    }
}
