using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Raises OS notifications for sync complete, conflict detected and sync error while the application is in the background.</summary>
public sealed class SyncNotificationService(ISyncEventAggregator syncEventAggregator, IAppActivityProvider appActivityProvider, INotificationSender notificationSender, IAccountNameResolver accountNameResolver, ILocalizationService loc)
{
    /// <summary>Starts listening for sync events.</summary>
    public void Start() => throw new NotImplementedException(string.Join(',', syncEventAggregator, appActivityProvider, notificationSender, accountNameResolver, loc));
}
