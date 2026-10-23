namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Reports whether the application is in the foreground.</summary>
public interface IAppActivityProvider
{
    /// <summary>Returns the current activity of the application.</summary>
    AppActivity Current();
}
