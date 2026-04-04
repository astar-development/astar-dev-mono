using AStar.Dev.Functional.Extensions;
using AStar.Dev.Sync.Engine.Features.ProgressTracking;

namespace AStar.Dev.Sync.Engine.Features.SyncOrchestration;

/// <summary>
///     Orchestrates a complete bi-directional sync run for a single account (SE-01 to SE-15).
///     All methods return <see cref="Result{TSuccess,TError}"/> — the engine never throws into callers (NF-16).
///     Registered as a singleton; thread-safe via the concurrency gate.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    ///     Starts a sync for <paramref name="accountId"/>.
    ///     When <paramref name="isFullResync"/> is <see langword="true"/>, the stored delta token is ignored.
    ///     Returns a completed <see cref="SyncReport"/> on success.
    ///     <para>
    ///         If another account is already syncing, <see cref="SyncReport.HadMultiAccountWarning"/> is <see langword="true"/>
    ///         (SE-05). The UI (S012) decides whether to prompt the user — the engine does not block.
    ///     </para>
    /// </summary>
    Task<Result<SyncReport, SyncEngineError>> StartSyncAsync(string accountId, bool isFullResync = false, CancellationToken ct = default);

    /// <summary>Returns the live progress observable for <paramref name="accountId"/> (SE-13, SE-14, NF-01).</summary>
    IObservable<SyncProgress> GetProgressStream(string accountId);
}
