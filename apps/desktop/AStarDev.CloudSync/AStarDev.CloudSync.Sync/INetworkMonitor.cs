namespace AStarDev.CloudSync.Sync;

/// <summary>Tells whether the operating system reports a network, and when that changes (#1462). The scheduler skips its ticks while there is none.</summary>
public interface INetworkMonitor
{
    /// <summary>Raised when the operating system reports that the network came or went. Raised on any thread.</summary>
    event EventHandler? Changed;

    /// <summary>What the operating system reports now.</summary>
    NetworkAvailability Availability { get; }
}
