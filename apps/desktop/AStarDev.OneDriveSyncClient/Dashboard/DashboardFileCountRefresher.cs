using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Dashboard;

/// <inheritdoc />
public sealed class DashboardFileCountRefresher(ISyncedItemRepository syncedItemRepository, ISyncEventAggregator syncEventAggregator, DashboardViewModel dashboard, ILogger<DashboardFileCountRefresher> logger) : IDashboardFileCountRefresher
{
    /// <inheritdoc />
    public void SubscribeToSyncEvents() => syncEventAggregator.SyncCompleted += OnSyncCompleted;

    /// <inheritdoc />
    public Task RefreshAsync(OneDriveAccount account, CancellationToken cancellationToken = default) => RefreshAsync(account.Id, cancellationToken);

    private async void OnSyncCompleted(object? sender, string accountId) => await RefreshAsync(new AccountId(accountId), CancellationToken.None);

    private async Task RefreshAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        try
        {
            int count = await syncedItemRepository.CountFilesAsync(accountId, cancellationToken).ConfigureAwait(false);
            dashboard.UpdateFileCount(accountId.Value, count);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.DashboardFileCountRefreshFailed(logger, accountId.Value, ex.GetType().Name);
        }
    }
}
