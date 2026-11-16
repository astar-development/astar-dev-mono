using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Clears the database migration lock a run that ended mid-migration left behind (#1788).</summary>
public interface IMigrationLockClearer
{
    /// <summary>Removes any migration lock. Call it only once this is the one running instance, before anything migrates, so no live migration holds the lock. Does nothing when the database or the lock does not exist yet.</summary>
    /// <param name="cancellationToken">Cancels the clear.</param>
    /// <returns><see cref="Unit"/> when no lock remains, or a failure that is already logged.</returns>
    Task<Result<Unit, ErrorResponse>> ClearAsync(CancellationToken cancellationToken);
}
