using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Supplies the data shown in the main window.</summary>
public interface ISyncOverviewSource
{
    /// <summary>Reads the current accounts, sync pairs, activity, conflicts and throughput.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The current overview, or an error when it could not be read.</returns>
    Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken);
}
