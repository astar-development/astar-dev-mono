using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Singleton that subscribes to <see cref="ISyncService"/> and <see cref="ISyncScheduler"/> events
/// and re-raises them via <see cref="IUiDispatcher"/> so child view models can receive UI-thread-safe
/// notifications without taking direct dependencies on those services.
/// </summary>
public sealed class SyncEventAggregator : ISyncEventAggregator
{
    private readonly IUiDispatcher _dispatcher;

    /// <inheritdoc />
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    /// <inheritdoc />
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;

    /// <inheritdoc />
    public event EventHandler<SyncConflict>? ConflictDetected;

    /// <inheritdoc />
    public event EventHandler<string>? SyncCompleted;

    public SyncEventAggregator(ISyncService syncService, ISyncScheduler scheduler, IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        syncService.SyncProgressChanged += OnSyncProgressChanged;
        syncService.JobCompleted += OnJobCompleted;
        syncService.ConflictDetected += OnConflictDetected;
        scheduler.SyncCompleted += OnSyncCompleted;
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs args) => _dispatcher.Post(() => SyncProgressChanged?.Invoke(this, args));

    private void OnJobCompleted(object? sender, JobCompletedEventArgs args) => _dispatcher.Post(() => JobCompleted?.Invoke(this, args));

    private void OnConflictDetected(object? sender, SyncConflict conflict) => _dispatcher.Post(() => ConflictDetected?.Invoke(this, conflict));

    private void OnSyncCompleted(object? sender, string accountId) => _dispatcher.Post(() => SyncCompleted?.Invoke(this, accountId));
}
