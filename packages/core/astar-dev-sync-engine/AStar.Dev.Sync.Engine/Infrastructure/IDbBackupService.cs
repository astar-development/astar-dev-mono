namespace AStar.Dev.Sync.Engine.Infrastructure;

/// <summary>
///     Creates a backup of the sync SQLite database before each sync run (EH-07).
///     Backup is best-effort: failure is logged at <c>Warning</c> and the sync proceeds.
/// </summary>
public interface IDbBackupService
{
    /// <summary>Backs up the database. Returns <see langword="true"/> if successful, <see langword="false"/> otherwise.</summary>
    Task<bool> BackupAsync(CancellationToken ct = default);
}
