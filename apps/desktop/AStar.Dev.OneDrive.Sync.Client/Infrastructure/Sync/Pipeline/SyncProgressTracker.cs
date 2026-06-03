using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Tracks per-job completion under a lock and fires progress and job-completed callbacks.
/// Progress events are throttled: <see cref="onProgress"/> fires once every
/// <paramref name="progressReportInterval"/> completions and always on the final job,
/// keeping UI dispatches bounded regardless of sync size.
/// <see cref="onJobCompleted"/> always fires for every job.
/// </summary>
internal sealed class SyncProgressTracker(int total, string accountId, string folderId, int progressReportInterval)
{
    private readonly Lock lockObj = new();
    private int done;

    internal int Done => done;

    internal void RecordCompletion(SyncJob job, bool success, string? error, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted)
    {
        int completedSoFar;
        bool shouldReportProgress;
        lock(lockObj)
        {
            done++;
            completedSoFar = done;
            shouldReportProgress = completedSoFar % progressReportInterval == 0 || completedSoFar == total;
        }

        var completedJob = success ? job.Complete() : job.Fail(error!);
        var syncState = completedSoFar == total ? SyncState.Idle : SyncState.Syncing;

        if (shouldReportProgress)
            onProgress(new SyncProgressEventArgs(accountId, folderId, completedSoFar, total, job.Target.RelativePath, syncState));

        onJobCompleted(new JobCompletedEventArgs(completedJob));
    }
}
