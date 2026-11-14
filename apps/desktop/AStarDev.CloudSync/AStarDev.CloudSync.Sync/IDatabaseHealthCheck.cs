using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Checks that the CloudSync database is not damaged (#1612). A damaged index or baseline must never be synced from.</summary>
public interface IDatabaseHealthCheck
{
    /// <summary>Runs the integrity check. A database file that does not exist yet is healthy: it is created fresh.</summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success when the database is sound, otherwise why it is not. It does not throw for a damaged file.</returns>
    Task<Result<Unit, DatabaseHealthFailure>> CheckAsync(CancellationToken cancellationToken);
}
