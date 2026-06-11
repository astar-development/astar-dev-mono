using System.Collections.Concurrent;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Runs scheduled sync passes for all connected accounts.
/// Default interval: 60 minutes. Configurable via Settings.
/// Manual sync can be triggered immediately via <see cref="TriggerNowAsync"/>.
/// </summary>
public sealed class SyncScheduler(ISyncService syncService, IAccountRepository accountRepository, ISyncRuleRepository syncRuleRepository, ILogger<SyncScheduler> logger) : IAsyncDisposable, ISyncScheduler
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> activeSyncs = new();
    private Timer? timer;
    private TimeSpan interval = TimeSpan.FromMinutes(60);
    private long runningFlag;

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
        this.interval = interval ?? DefaultInterval;
        timer?.Dispose();

        try
        {
            timer = new Timer(OnTimerTickAsync, state: null, dueTime: this.interval, period: this.interval);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncSchedulerTimerFatal(logger, ex.Message, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public void StopSync() => timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    /// <inheritdoc />
    public void SetInterval(TimeSpan interval)
    {
        this.interval = interval;
        _ = (timer?.Change(interval, interval));
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
    {
        var accountOption = await accountRepository.GetByIdAsync(new AccountId(accountId), ct).ConfigureAwait(false);

        await accountOption.Match<Task>(
            async entity =>
            {
                var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, ct).ConfigureAwait(false);
                await TriggerAccountAsync(MapEntityToAccount(entity, rules), ct).ConfigureAwait(false);
            },
            () =>
            {
                OneDriveSyncClientMessages.SyncSchedulerUnknownAccount(logger, accountId);
                return Task.CompletedTask;
            });
    }

    /// <inheritdoc />
    public async Task TriggerAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (!activeSyncs.TryAdd(account.Id.Id, cts))
        {
            OneDriveSyncClientMessages.SyncSchedulerSkippedAlreadyRunning(logger, account.Id.Id);
            return;
        }

        SyncStarted?.Invoke(this, account.Id.Id);
        try
        {
            await syncService.SyncAccountAsync(account, cts.Token);
        }
        finally
        {
            activeSyncs.TryRemove(account.Id.Id, out _);
            SyncCompleted?.Invoke(this, account.Id.Id);
        }
    }

    /// <inheritdoc />
    public Task CancelAccountSyncAsync(string accountId)
    {
        if (activeSyncs.TryGetValue(accountId, out var cts))
        {
            OneDriveSyncClientMessages.SyncSchedulerCancelled(logger, accountId);
            cts.Cancel();
        }

        return Task.CompletedTask;
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
            OneDriveSyncClientMessages.SyncSchedulerTimerError(logger, ex.Message, ex);
        }
    }

    private bool SyncIsAlreadyRunning() => Interlocked.Read(ref runningFlag) == 1 || !activeSyncs.IsEmpty;

    private static OneDriveAccount MapEntityToAccount(AccountEntity entity, IReadOnlyList<SyncRuleEntity> rules) => new()
    {
        Id = entity.Id,
        Profile = entity.Profile,
        AccentIndex = entity.AccentIndex,
        IsActive = entity.IsActive,
        LastSyncedAt = entity.LastSyncedAt,
        SyncConfig = entity.SyncConfig.LocalSyncPath.Value.Length > 0 ? Option.Some(entity.SyncConfig) : Option.None<AccountSyncConfig>(),
        SelectedFolderIds = [.. rules.Where(r => r.RuleType == RuleType.Include).Choose(r => r.RemoteItemId).Select(id => new OneDriveFolderId(id))]
    };

    private async Task RunSyncPassAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref runningFlag, 1) == 1)
            return;

        try
        {
            var entities = await accountRepository.GetAllAsync(CancellationToken.None);
            foreach (var entity in entities.TakeWhile(_ => !ct.IsCancellationRequested))
            {
                var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, ct).ConfigureAwait(false);
                var account = MapEntityToAccount(entity, rules);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (!activeSyncs.TryAdd(account.Id.Id, cts))
                {
                    OneDriveSyncClientMessages.SyncSchedulerSkippedAlreadyRunning(logger, account.Id.Id);
                    continue;
                }

                SyncStarted?.Invoke(this, account.Id.Id);
                try
                {
                    await syncService.SyncAccountAsync(account, cts.Token);
                }
                catch (Exception ex)
                {
                    OneDriveSyncClientMessages.SyncSchedulerFailed(logger, account.Id.Id, ex.Message, ex);
                }
                finally
                {
                    activeSyncs.TryRemove(account.Id.Id, out _);
                    SyncCompleted?.Invoke(this, account.Id.Id);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref runningFlag, 0);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        StopSync();

        foreach (var cts in activeSyncs.Values)
            cts.Cancel();

        activeSyncs.Clear();

        if (timer is not null)
            await timer.DisposeAsync();
    }
}
