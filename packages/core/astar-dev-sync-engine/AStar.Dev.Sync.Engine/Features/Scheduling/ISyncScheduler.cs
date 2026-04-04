namespace AStar.Dev.Sync.Engine.Features.Scheduling;

/// <summary>
///     Drives scheduled sync runs per account based on a configured interval (SE-04, SE-06).
///     Skips a tick if a sync for that account is already running.
///     Respects <see cref="CancellationToken"/> from <c>IHostApplicationLifetime</c> for clean shutdown.
/// </summary>
public interface ISyncScheduler
{
    /// <summary>Starts the scheduler loop. Completes when <paramref name="ct"/> is cancelled.</summary>
    Task StartAsync(CancellationToken ct = default);
}
