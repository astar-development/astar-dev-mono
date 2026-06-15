using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Tracks per-job completion under a lock and fires progress and job-completed callbacks.
/// Progress events are throttled: <see cref="onProgress"/> fires once every
/// <paramref name="progressReportInterval"/> completions and always on the final job,
/// keeping UI dispatches bounded regardless of sync size.
/// <see cref="onJobCompleted"/> always fires for every job.
/// Per-job progress events always carry <see cref="SyncState.Syncing"/>; the caller
/// is responsible for emitting the terminal <see cref="SyncState.Idle"/> event after
/// all workers have finished.
/// </summary>
internal sealed class SyncProgressTracker(string accountId, string folderId, int progressReportInterval)
{
    private readonly Lock lockObj = new();
    private int done;
    private int total = int.MaxValue;
    private bool totalized;

    internal int Done => done;

    internal void SetTotal(int value)
    {
        lock (lockObj)
        {
            total = value;
            totalized = true;
        }
    }

    internal void RecordCompletion(SyncJob job, bool success, string? error, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted)
    {
        int completedSoFar;
        bool shouldReportProgress;
        int currentTotal;
        bool isTotalized;
        lock (lockObj)
        {
            done++;
            completedSoFar = done;
            currentTotal = total;
            isTotalized = totalized;
            shouldReportProgress = completedSoFar % progressReportInterval == 0 || (isTotalized && completedSoFar == currentTotal);
        }

        var completedJob = success ? job.Complete() : job.Fail(error!);

        if (shouldReportProgress)
            onProgress(new SyncProgressEventArgs(accountId, folderId, completedSoFar, currentTotal, job.Target.RelativePath, SyncState.Syncing));

        onJobCompleted(new JobCompletedEventArgs(completedJob));
    }
}
