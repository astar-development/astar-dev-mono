namespace AStarDev.CloudSync.Sync;

/// <summary>What the user chose when they pressed Sync Now while a run was in progress (SE-08, #1402).</summary>
public abstract record SyncRestartChoice
{
    private SyncRestartChoice()
    {
    }

    /// <summary>"Cancel the current sync and start again": cancel the running run, then start a manual run.</summary>
    public sealed record CancelAndRestart : SyncRestartChoice
    {
        internal CancelAndRestart()
        {
        }
    }

    /// <summary>"Keep the current sync": change nothing, and do not queue another run.</summary>
    public sealed record KeepCurrent : SyncRestartChoice
    {
        internal KeepCurrent()
        {
        }
    }
}
