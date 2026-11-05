using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Core.Persistence;

/// <summary>The CloudSync database as a whole.</summary>
public interface ICloudSyncDatabase
{
    /// <summary>Creates the database if it is missing and applies any pending migrations. Safe to run again; it does nothing when the database is up to date. The overview runs it before reading accounts, so a failure is shown to the user.</summary>
    /// <param name="cancellationToken">Cancels the migration.</param>
    /// <returns><see cref="Unit"/> when the database is ready, or a failure that is already logged.</returns>
    Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken);
}
