namespace AStarDev.CloudSync.Core;

/// <summary>Supplies the data shown in the main window.</summary>
public interface ISyncOverviewSource
{
    /// <summary>Reads the current accounts, sync pairs, activity, conflicts and throughput.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The current overview.</returns>
    Task<SyncOverview> GetOverviewAsync(CancellationToken cancellationToken);
}
