using System.Threading;
using System.Threading.Tasks;
using SyncEngineBackup = AStar.Dev.Sync.Engine.Infrastructure.IDbBackupService;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Adapts the desktop app's <see cref="IDbBackupService"/> to the interface expected by the sync engine package
///     (<see cref="AStar.Dev.Sync.Engine.Infrastructure.IDbBackupService"/>) (EH-07).
///     The desktop interface returns <c>Result&lt;bool, ErrorResponse&gt;</c>; the engine interface returns <c>bool</c>.
/// </summary>
internal sealed class SyncEngineDbBackupAdapter(IDbBackupService desktopBackupService) : SyncEngineBackup
{
    /// <inheritdoc />
    public async Task<bool> BackupAsync(CancellationToken ct = default)
    {
        var result = await desktopBackupService.BackupAsync(ct).ConfigureAwait(false);

        return result.Match(success => success, _ => false);
    }
}
