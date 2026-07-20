namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <summary>Checks for an application update and, if one is found, presents the user with the update-available dialog.</summary>
public interface IUpdateNotificationService
{
    /// <summary>Checks for updates and shows the dialog if one is available. No-op if none is found or the check fails.</summary>
    Task CheckAndNotifyAsync(CancellationToken cancellationToken = default);
}
