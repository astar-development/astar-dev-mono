using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Backs up the SQLite database before a sync mutation begins (AC EH-07).
///
///     Backup is <strong>not</strong> periodic — it is triggered only on explicit call
///     from the sync orchestration layer.
/// </summary>
public interface IDbBackupService
{
    /// <summary>
    ///     Copies <c>data.db</c> to <c>data.db.bak</c> in the application data directory.
    /// </summary>
    /// <param name="cancellationToken">
    ///     Token used to cancel the file-copy operation mid-stream.
    /// </param>
    /// <returns>
    ///     <see cref="Result{TSuccess,TError}.Ok" /> wrapping <see langword="true" /> on success,
    ///     or <see cref="Result{TSuccess,TError}.Error" /> if <c>data.db</c> does not exist.
    /// </returns>
    Task<Result<bool, ErrorResponse>> BackupAsync(CancellationToken cancellationToken = default);
}
