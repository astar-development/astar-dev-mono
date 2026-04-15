using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Runs scheduled sync passes for all connected accounts.
/// Default interval: 60 minutes. Configurable via Settings.
/// Manual sync can be triggered immediately via <see cref="TriggerNowAsync"/>.
/// </summary>
public sealed class SyncScheduler(ISyncService syncService, IAccountRepository accountRepository) : IAsyncDisposable, ISyncScheduler
{
    private Timer? _timer;
    private TimeSpan _interval = TimeSpan.FromMinutes(60);
    private int _runningFlag;

    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(60);

    public event EventHandler<string>? SyncStarted;
    public event EventHandler<string>? SyncCompleted;

    public void StartSync(TimeSpan? interval = null)
    {
        _interval = interval ?? DefaultInterval;

        try
        {
            _timer = new Timer(OnTimerTickAsync, state: null, dueTime: _interval, period: _interval);
        }
        catch(Exception ex)
        {
            Serilog.Log.Fatal(ex, "[SyncScheduler.Start] FATAL ERROR creating Timer: {Error}", ex.Message);
            throw;
        }
    }

    public void StopSync() => _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    public void SetInterval(TimeSpan interval)
    {
        _interval = interval;
        _ = (_timer?.Change(interval, interval));
    }

    /// <summary>
    /// Triggers an immediate sync for all accounts outside the normal schedule.
    /// </summary>
    public async Task TriggerNowAsync(CancellationToken ct = default)
    {
        if(_runningFlag == 1)
            return;

        await RunSyncPassAsync(ct);
    }

    /// <summary>
    /// Triggers an immediate sync for a single account identified by its raw string ID.
    /// </summary>
    public async Task TriggerAccountAsync(string accountId, CancellationToken ct = default)
    {
        var entity = await accountRepository.GetByIdAsync(new AccountId(accountId), ct).ConfigureAwait(false);
        if(entity is null)
            return;

        var account = new OneDriveAccount
        {
            Id                = entity.Id,
            DisplayName       = entity.DisplayName,
            Email             = entity.Email,
            LocalSyncPath     = entity.LocalSyncPath.Value.Length > 0 ? entity.LocalSyncPath : null,
            ConflictPolicy    = entity.ConflictPolicy,
            SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)],
            LastSyncedAt      = entity.LastSyncedAt
        };

        await TriggerAccountAsync(account, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Triggers an immediate sync for a single account.
    /// </summary>
    public async Task TriggerAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        SyncStarted?.Invoke(this, account.Id.Id);
        try
        {
            await syncService.SyncAccountAsync(account, ct);
        }
        finally
        {
            SyncCompleted?.Invoke(this, account.Id.Id);
        }
    }

    // ReSharper disable once AsyncVoidMethod - Timer requires this signature
    private async void OnTimerTickAsync(object? state)
    {
        if(_runningFlag == 1)
            return;

        await RunSyncPassAsync(CancellationToken.None);
    }

    private async Task RunSyncPassAsync(CancellationToken ct)
    {
        if(Interlocked.Exchange(ref _runningFlag, 1) == 1)
            return;

        try
        {
            var entities = await accountRepository.GetAllAsync(CancellationToken.None);
            foreach(var account in entities.TakeWhile(_ => !ct.IsCancellationRequested).Select(entity => new OneDriveAccount
            {
                Id                = entity.Id,
                DisplayName       = entity.DisplayName,
                Email             = entity.Email,
                AccentIndex       = entity.AccentIndex,
                IsActive          = entity.IsActive,
                LocalSyncPath     = entity.LocalSyncPath.Value.Length > 0 ? entity.LocalSyncPath : null,
                ConflictPolicy    = entity.ConflictPolicy,
                SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)]
            }))
            {
                SyncStarted?.Invoke(this, account.Id.Id);
                try
                {
                    await syncService.SyncAccountAsync(account, ct);
                }
                catch(Exception ex)
                {
                    Serilog.Log.Error(ex, "[SyncScheduler] Scheduled sync failed for {Id}: {Error}", account.Id.Id, ex.Message);
                }
                finally
                {
                    SyncCompleted?.Invoke(this, account.Id.Id);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _runningFlag, 0);
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopSync();

        if(_timer is not null)
            await _timer.DisposeAsync();
    }
}
