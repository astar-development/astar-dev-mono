using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

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
    private int failed;
    private int total = int.MaxValue;
    private bool totalized;
    private int lastReportedDone;
    private string lastCompletedFile = string.Empty;

    internal int Done => done;
    internal int FailedCount => failed;

    internal void SetTotal(int value, Action<SyncProgressEventArgs>? onProgress = null)
    {
        SyncProgressEventArgs? missedFinalProgress = null;

        lock (lockObj)
        {
            total = value;
            totalized = true;

            if (done == value && done > lastReportedDone)
            {
                lastReportedDone = done;
                missedFinalProgress = new SyncProgressEventArgs(accountId, folderId, done, value, lastCompletedFile, SyncState.Syncing);
            }
        }

        if (missedFinalProgress is not null)
            onProgress?.Invoke(missedFinalProgress);
    }

    internal async Task RecordCompletionAsync(SyncJob job, bool success, string? error, Action<SyncProgressEventArgs> onProgress, Func<JobCompletedEventArgs, Task> onJobCompleted)
    {
        int completedSoFar;
        bool shouldReportProgress;
        int currentTotal;
        bool isTotalized;
        lock (lockObj)
        {
            done++;
            if (!success)
                failed++;
            completedSoFar = done;
            currentTotal = total;
            isTotalized = totalized;
            shouldReportProgress = completedSoFar % progressReportInterval == 0 || (isTotalized && completedSoFar == currentTotal);
            lastCompletedFile = job.Target.RelativePath;
            if (shouldReportProgress)
                lastReportedDone = completedSoFar;
        }

        var completedJob = success ? job.Complete() : job.Fail(error ?? throw new ArgumentNullException(nameof(error), "Error message required when success is false."));

        if (shouldReportProgress)
            onProgress(new SyncProgressEventArgs(accountId, folderId, completedSoFar, currentTotal, job.Target.RelativePath, SyncState.Syncing));

        await onJobCompleted(new JobCompletedEventArgs(completedJob)).ConfigureAwait(false);
    }
}
