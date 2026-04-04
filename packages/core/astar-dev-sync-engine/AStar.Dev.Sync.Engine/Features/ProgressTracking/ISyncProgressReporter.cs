namespace AStar.Dev.Sync.Engine.Features.ProgressTracking;

/// <summary>
///     Publishes real-time sync progress for a given account without polling (SE-13, SE-14, NF-01).
///     The UI subscribes to <see cref="GetProgressStream"/> and applies
///     <c>.ObserveOn(RxApp.MainThreadScheduler)</c> at the subscription site — this interface is UI-thread-agnostic.
/// </summary>
public interface ISyncProgressReporter
{
    /// <summary>Returns a hot observable that emits progress snapshots for <paramref name="accountId"/>.</summary>
    IObservable<SyncProgress> GetProgressStream(string accountId);

    /// <summary>Publishes a new <paramref name="progress"/> snapshot for <paramref name="accountId"/>.</summary>
    void Report(string accountId, SyncProgress progress);
}
