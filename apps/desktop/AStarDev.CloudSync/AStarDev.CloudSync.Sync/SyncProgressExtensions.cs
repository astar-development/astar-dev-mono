namespace AStarDev.CloudSync.Sync;

/// <summary>Reads <see cref="SyncProgress"/> values.</summary>
public static class SyncProgressExtensions
{
    /// <summary>The whole percentage of the phase that is done, from 0 to 100; 0 when the total is unknown.</summary>
    /// <param name="progress">The progress.</param>
    public static int PercentComplete(this SyncProgress progress) => progress.BytesTotal > 0 ? (int)(progress.BytesDone * 100 / progress.BytesTotal) : 0;
}
