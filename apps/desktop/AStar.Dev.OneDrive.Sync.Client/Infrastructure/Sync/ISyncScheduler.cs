using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public interface ISyncScheduler
{
    /// <summary>
    /// Runs scheduled sync passes for all connected accounts.
    /// </summary>
    event EventHandler<string>? SyncStarted;

    /// <summary>
    /// Raised when a sync pass completes for an account. The string parameter is the account ID.
    /// </summary>
    event EventHandler<string>? SyncCompleted;

    /// <summary>
    /// Starts the sync scheduler with the specified interval. If no interval is provided, uses the default (60 minutes).
    /// </summary>
    /// <param name="interval">The interval at which to run sync passes.</param>
    void StartSync(TimeSpan? interval = null);

    /// <summary>
    /// Stops the sync scheduler, halting all scheduled sync passes until restarted. Does not affect manual syncs triggered via TriggerNowAsync or TriggerAccountAsync.
    /// </summary>
    void StopSync();

    /// <summary>
    /// Updates the sync interval for scheduled sync passes. If the scheduler is currently running, it will apply the new interval immediately.
    /// </summary>
    /// <param name="interval">The new interval for scheduled sync passes.</param>
    void SetInterval(TimeSpan interval);

    /// <summary>
    /// Triggers an immediate sync for all accounts outside the normal schedule.
    /// </summary>
    Task TriggerNowAsync(CancellationToken ct = default);

    /// <summary>
    /// Triggers an immediate sync for a single account identified by its ID.
    /// </summary>
    Task TriggerAccountAsync(string accountId, CancellationToken ct = default);

    /// <summary>
    /// Triggers an immediate sync for a single account.
    /// </summary>
    Task TriggerAccountAsync(OneDriveAccount account, CancellationToken ct = default);

    /// <summary>
    /// Performs any necessary cleanup when the scheduler is no longer needed, such as disposing of timers or other resources. After disposal, the scheduler should not be used to trigger syncs or raise events.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask DisposeAsync();
}
