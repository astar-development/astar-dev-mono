using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>Tracks per-job completion under a lock and fires progress and job-completed callbacks.</summary>
internal sealed class SyncProgressTracker(int total, string accountId, string folderId)
{
    private readonly Lock lockObj = new();
    private int done;

    internal int Done => done;

    internal void RecordCompletion(SyncJob job, bool success, string? error, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted)
    {
        int completedSoFar;
        lock(lockObj)
        {
            done++;
            completedSoFar = done;
        }

        var completedJob = success ? job.Complete() : job.Fail(error);
        var syncState = completedSoFar == total ? SyncState.Idle : SyncState.Syncing;

        onProgress(new SyncProgressEventArgs(accountId, folderId, completedSoFar, total, job.Target.RelativePath, syncState));
        onJobCompleted(new JobCompletedEventArgs(completedJob));
    }
}
