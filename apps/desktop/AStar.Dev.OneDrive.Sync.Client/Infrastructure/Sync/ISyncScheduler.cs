using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public interface ISyncScheduler
{
    event EventHandler<string>? SyncStarted;
    event EventHandler<string>? SyncCompleted;
    void Start(TimeSpan? interval = null);
#pragma warning disable CA1716
    void Stop();
#pragma warning restore CA1716
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

    ValueTask DisposeAsync();
}
