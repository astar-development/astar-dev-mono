namespace AStarDev.CloudSync.Sync;

/// <summary>What the user chose when Sync Now was pressed while a sync was already running (SE-08, #1402).</summary>
public abstract record SyncRestartChoice
{
    private SyncRestartChoice()
    {
    }

    /// <summary>Cancel the running sync and start again.</summary>
    public sealed record CancelAndRestart : SyncRestartChoice
    {
        internal CancelAndRestart()
        {
        }
    }

    /// <summary>Keep the running sync; nothing changes.</summary>
    public sealed record KeepCurrent : SyncRestartChoice
    {
        internal KeepCurrent()
        {
        }
    }
}
