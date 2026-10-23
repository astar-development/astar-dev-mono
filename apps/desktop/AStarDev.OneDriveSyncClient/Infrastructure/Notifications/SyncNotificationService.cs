using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Raises OS notifications for sync complete, conflict detected and sync error while the application is in the background.</summary>
public sealed class SyncNotificationService(ISyncEventAggregator syncEventAggregator, IAppActivityProvider appActivityProvider, INotificationSender notificationSender, IAccountNameResolver accountNameResolver, ILocalizationService loc)
{
    private readonly HashSet<string> erroredAccountIds = [];

    /// <summary>Starts listening for sync events.</summary>
    public void Start()
    {
        syncEventAggregator.SyncCompleted += OnSyncCompleted;
        syncEventAggregator.ConflictDetected += OnConflictDetected;
        syncEventAggregator.SyncProgressChanged += OnSyncProgressChanged;
    }

    private void OnSyncCompleted(object? sender, string accountId)
    {
        if (erroredAccountIds.Remove(accountId))
            return;

        Notify(loc.GetLocal("Notification.SyncComplete.Title"), loc.GetLocal("Notification.SyncComplete.Body", accountNameResolver.Resolve(accountId)));
    }

    private void OnConflictDetected(object? sender, SyncConflict conflict)
    {
        string fileName = Path.GetFileName(conflict.Target.RelativePath);
        string accountName = accountNameResolver.Resolve(conflict.Remote.AccountId.Value);

        Notify(loc.GetLocal("Notification.Conflict.Title"), loc.GetLocal("Notification.Conflict.Body", fileName, accountName));
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs args)
    {
        if (args.SyncState is not SyncState.Error)
            return;

        if (!erroredAccountIds.Add(args.AccountId))
            return;

        Notify(loc.GetLocal("Notification.SyncError.Title"), loc.GetLocal("Notification.SyncError.Body", accountNameResolver.Resolve(args.AccountId), args.CurrentFile));
    }

    private void Notify(string title, string body)
    {
        if (appActivityProvider.Current() is not AppActivity.Background)
            return;

        _ = notificationSender.SendAsync(DesktopNotificationFactory.Create(title, body));
    }
}
