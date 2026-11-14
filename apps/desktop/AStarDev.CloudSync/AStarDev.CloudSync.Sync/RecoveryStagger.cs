namespace AStarDev.CloudSync.Sync;

/// <summary>How startup recovery spreads its passes (#1610).</summary>
public static class RecoveryStagger
{
    /// <summary>The recovery passes of the interrupted accounts start evenly across this window, so they do not all hit the network and the disk at once.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
}
