namespace AStarDev.CloudSync.Sync;

/// <summary>Starts a sync pass for each account on the schedule (SE-04 and SE-05, #1403). Paused, sign-in-required and root-unavailable accounts are skipped and the reason is logged. Missed ticks, after sleep or a busy app, give one pass.</summary>
public interface ISyncScheduler
{
    /// <summary>Starts watching the schedule. The first pass comes after a short delay, plus the account's offset, so startup stays fast. Calling it again does nothing.</summary>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>A task that completes when the scheduler is running.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Stops watching the schedule and cancels the passes it started, then waits for them to end. No pass starts after it. Calling it when not started does nothing.</summary>
    /// <param name="cancellationToken">Stops waiting for the passes; they are still cancelled.</param>
    /// <returns>A task that completes when the passes have ended.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
