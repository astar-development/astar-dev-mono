using System.Collections.Concurrent;
using System.Reactive;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

/// <summary>
/// Runs scheduled sync passes for all connected accounts.
/// Default interval: 60 minutes. Configurable via Settings.
/// Manual sync can be triggered immediately via <see cref="TriggerNowAsync"/>.
/// </summary>
public sealed class SyncScheduler(ISyncService syncService, IAccountRepository accountRepository, ISyncRuleRepository syncRuleRepository, ILogger<SyncScheduler> logger) : IAsyncDisposable, ISyncScheduler
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> activeSyncs = new();
    private readonly SemaphoreSlim fullPassSemaphore = new(1, 1);
    private Timer? timer;

    /// <summary>
    /// Default interval for scheduled sync passes. Can be overridden by providing a different interval to StartSync or SetInterval.
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(60);

    /// <inheritdoc />
    public event EventHandler<string>? SyncStarted;

    /// <inheritdoc />
    public event EventHandler<string>? SyncCompleted;

    /// <inheritdoc />
    public Result<Unit, string> StartSync(TimeSpan? interval = null)
    {
        timer?.Dispose();

        try
        {
            timer = new Timer(OnTimerTickAsync, state: null, dueTime: interval ?? DefaultInterval, period: interval ?? DefaultInterval);

            return new Ok<Unit, string>(Unit.Default);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncSchedulerTimerFatal(logger, ex.Message, ex);

            return new Fail<Unit, string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public void StopSync() => timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    /// <inheritdoc />
    public void SetInterval(TimeSpan interval) => _ = (timer?.Change(interval, interval));

    /// <inheritdoc />
    public async Task TriggerNowAsync(CancellationToken cancellationToken = default)
    {
        if (!activeSyncs.IsEmpty)
            return;

        await RunSyncPassAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TriggerAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var accountOption = await accountRepository.GetByIdAsync(new AccountId(accountId), cancellationToken).ConfigureAwait(false);

        await accountOption.Match(
            async entity =>
            {
                var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, cancellationToken).ConfigureAwait(false);
                await TriggerAccountAsync(MapEntityToAccount(entity, rules), cancellationToken).ConfigureAwait(false);
            },
            () =>
            {
                OneDriveSyncClientMessages.SyncSchedulerUnknownAccount(logger, accountId);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TriggerAccountAsync(OneDriveAccount account, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!activeSyncs.TryAdd(account.Id.Value, cts))
        {
            OneDriveSyncClientMessages.SyncSchedulerSkippedAlreadyRunning(logger, account.Id.Value);
            return;
        }

        SyncStarted?.Invoke(this, account.Id.Value);
        try
        {
            await syncService.SyncAccountAsync(account, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            activeSyncs.TryRemove(account.Id.Value, out _);
            SyncCompleted?.Invoke(this, account.Id.Value);
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
        if (!activeSyncs.IsEmpty)
            return;

        try
        {
            await RunSyncPassAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncSchedulerTimerError(logger, ex.Message, ex);
        }
    }

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

    private async Task RunSyncPassAsync(CancellationToken cancellationToken)
    {
        if (!await fullPassSemaphore.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
            return;

        try
        {
            var entities = await accountRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            foreach (var entity in entities.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            {
                var rules = await syncRuleRepository.GetByAccountIdAsync(entity.Id, cancellationToken).ConfigureAwait(false);
                var account = MapEntityToAccount(entity, rules);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (!activeSyncs.TryAdd(account.Id.Value, cts))
                {
                    OneDriveSyncClientMessages.SyncSchedulerSkippedAlreadyRunning(logger, account.Id.Value);
                    continue;
                }

                SyncStarted?.Invoke(this, account.Id.Value);
                try
                {
                    await syncService.SyncAccountAsync(account, cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    OneDriveSyncClientMessages.SyncSchedulerFailed(logger, account.Id.Value, ex.Message, ex);
                }
                finally
                {
                    activeSyncs.TryRemove(account.Id.Value, out _);
                    SyncCompleted?.Invoke(this, account.Id.Value);
                }
            }
        }
        finally
        {
            fullPassSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        StopSync();

        foreach (var cts in activeSyncs.Values)
            await cts.CancelAsync().ConfigureAwait(false);

        activeSyncs.Clear();

        if (timer is not null)
            await timer.DisposeAsync().ConfigureAwait(false);

        fullPassSemaphore.Dispose();
    }
}
