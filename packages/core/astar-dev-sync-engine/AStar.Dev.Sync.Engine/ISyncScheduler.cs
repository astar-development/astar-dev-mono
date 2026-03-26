namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Manages automatic sync scheduling with per-account staggering (SE-04, SE-05).
/// </summary>
public interface ISyncScheduler : IAsyncDisposable
{
    /// <summary>
    /// Starts automatic sync scheduling for the given accounts.
    /// Schedules are staggered evenly across the sync interval (SE-05).
    /// </summary>
    /// <param name="accountIds">The accounts to schedule.</param>
    /// <param name="ct">Cancellation token that stops all scheduled syncs.</param>
    Task StartAsync(IReadOnlyList<string> accountIds, CancellationToken ct = default);

    /// <summary>
    /// Stops all scheduled syncs.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Returns whether the scheduler is currently running.
    /// </summary>
    bool IsRunning { get; }
}
