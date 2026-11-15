using AStar.Dev.Velopack.Publishing;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Updates;

/// <summary>Holds an update back until no sync is running, and keeps new passes out until the app restarts (#1412, UP-06). It uses the account lock's drain, so a pass that is refused says it is paused for an update.</summary>
/// <param name="syncLock">Waits for running passes and refuses new ones.</param>
public sealed class CloudSyncUpdateApplyGate(IAccountSyncLock syncLock) : IUpdateApplyGate
{
    /// <inheritdoc />
    public async Task<IDisposable> WaitUntilSafeAsync(CancellationToken cancellationToken) => await syncLock.DrainAsync(cancellationToken).ConfigureAwait(false);
}
