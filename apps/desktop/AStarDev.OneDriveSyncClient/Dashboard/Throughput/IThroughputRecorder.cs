namespace AStarDev.OneDriveSyncClient.Dashboard.Throughput;

/// <summary>Records the size of every completed transfer so it can be graphed.</summary>
public interface IThroughputRecorder
{
    /// <summary>Subscribes to job completion events.</summary>
    void SubscribeToSyncEvents();
}
