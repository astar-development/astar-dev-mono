using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Runs scheduled sync passes for all connected accounts.
/// Default interval: 60 minutes. Configurable via Settings.
/// Manual sync can be triggered immediately via <see cref="TriggerNowAsync"/>.
/// </summary>
public sealed class SyncScheduler(ISyncService syncService, IAccountRepository accountRepository, ISyncRuleRepository syncRuleRepository) : IAsyncDisposable, ISyncScheduler
{
    private Timer? _timer;
    private TimeSpan _interval = TimeSpan.FromMinutes(60);
    private long _runningFlag;

    /// <summary>
    /// Default interval for scheduled sync passes. Can be overridden by providing a different interval to StartSync or SetInterval.
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(60);

    /// <inheritdoc />
    public event EventHandler<string>? SyncStarted;

    /// <inheritdoc />
    public event EventHandler<string>? SyncCompleted;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void StopSync() => _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    /// <inheritdoc />
    public void SetInterval(TimeSpan interval)
    {
        _interval = interval;
        _ = (_timer?.Change(interval, interval));
    }

    /// <inheritdoc />
    public async Task TriggerNowAsync(CancellationToken ct = default)
    {
        if (SyncIsAlreadyRunning())
            return;

        await RunSyncPassAsync(ct);
    }

    /// <inheritdoc />
    public async Task TriggerAccountAsync(string accountId, CancellationToken ct = default)
        => await accountRepository.GetByIdAsync(new AccountId(accountId), ct)
            .TapAsync(async entity =>
            {
                var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, ct);
                await TriggerAccountAsync(MapEntityToAccount(entity, rules), ct);
            });

    /// <inheritdoc />
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
        if (SyncIsAlreadyRunning())
            return;

        try
        {
            await RunSyncPassAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "[SyncScheduler] Unhandled exception in timer callback: {Error}", ex.Message);
        }
    }

    private bool SyncIsAlreadyRunning() => Interlocked.Read(ref _runningFlag) == 1;

    private static OneDriveAccount MapEntityToAccount(AccountEntity entity, IReadOnlyList<SyncRuleEntity> rules) => new()
    {
        Id                = entity.Id,
        Profile           = entity.Profile,
        AccentIndex       = entity.AccentIndex,
        IsActive          = entity.IsActive,
        LastSyncedAt      = entity.LastSyncedAt,
        SyncConfig        = entity.SyncConfig.LocalSyncPath.Value.Length > 0 ? entity.SyncConfig : null,
        SelectedFolderIds = [.. rules.Where(r => r.RuleType == RuleType.Include && r.RemoteItemId is not null).Select(r => new OneDriveFolderId(r.RemoteItemId!))]
    };

    private async Task RunSyncPassAsync(CancellationToken ct)
    {
        if(Interlocked.Exchange(ref _runningFlag, 1) == 1)
            return;

        try
        {
            var entities = await accountRepository.GetAllAsync(CancellationToken.None);
            foreach(var entity in entities.TakeWhile(_ => !ct.IsCancellationRequested))
            {
                var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, ct).ConfigureAwait(false);
                var account = MapEntityToAccount(entity, rules);

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
