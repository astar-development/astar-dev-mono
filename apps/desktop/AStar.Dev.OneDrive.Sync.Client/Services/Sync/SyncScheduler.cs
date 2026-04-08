using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

/// <summary>
/// Runs scheduled sync passes for all connected accounts.
/// Default interval: 60 minutes. Configurable via Settings.
/// Manual sync can be triggered immediately via <see cref="TriggerNowAsync"/>.
/// </summary>
public sealed class SyncScheduler(ISyncService syncService, IAccountRepository accountRepository) : IAsyncDisposable
{
    private readonly ISyncService       _syncService       = syncService;
    private readonly IAccountRepository _accountRepository = accountRepository;
    private          Timer?             _timer;
    private          TimeSpan           _interval = TimeSpan.FromMinutes(60);
    private          bool               _running;

    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(60);

    public event EventHandler<string>? SyncStarted;
    public event EventHandler<string>? SyncCompleted;

    public void Start(TimeSpan? interval = null)
    {
        _interval = interval ?? DefaultInterval;

        try
        {
            _timer = new Timer(OnTimerTick, state: null, dueTime: _interval, period: _interval);

            _running = false;
        }
        catch(Exception ex)
        {
            Serilog.Log.Fatal(ex, "[SyncScheduler.Start] FATAL ERROR creating Timer: {Error}", ex.Message);
            throw;
        }
    }

    public void Stop() => _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

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
        if(_running)
            return;

        await RunSyncPassAsync(ct);
    }

    /// <summary>
    /// Triggers an immediate sync for a single account.
    /// </summary>
    public async Task TriggerAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        SyncStarted?.Invoke(this, account.Id);
        try
        {
            await _syncService.SyncAccountAsync(account, ct);
        }
        finally
        {
            SyncCompleted?.Invoke(this, account.Id);
        }
    }

    private async void OnTimerTick(object? state)
    {
        if(_running)
            return;

        await RunSyncPassAsync(CancellationToken.None);
    }

    private async Task RunSyncPassAsync(CancellationToken ct)
    {
        _running = true;

        try
        {
            var entities = await _accountRepository.GetAllAsync();
            foreach(var entity in entities)
            {
                if(ct.IsCancellationRequested)
                    break;

                var account = new OneDriveAccount
                {
                    Id                = entity.Id,
                    DisplayName       = entity.DisplayName,
                    Email             = entity.Email,
                    AccentIndex       = entity.AccentIndex,
                    IsActive          = entity.IsActive,
                    LocalSyncPath     = entity.LocalSyncPath,
                    ConflictPolicy    = entity.ConflictPolicy,
                    SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)]
                };

                SyncStarted?.Invoke(this, account.Id);
                try
                {
                    await _syncService.SyncAccountAsync(account, ct);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Scheduled sync failed for {account.Email}: {ex.Message}");
                }
                finally
                {
                    SyncCompleted?.Invoke(this, account.Id);
                }
            }
        }
        finally
        {
            _running = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Stop();

        if(_timer is not null)
            await _timer.DisposeAsync();
    }
}
