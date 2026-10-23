namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Creates <see cref="AppActivity"/> instances.</summary>
public static class AppActivityFactory
{
    /// <summary>Creates the activity for a visible, focused main window.</summary>
    public static AppActivity CreateForeground() => new AppActivity.Foreground();

    /// <summary>Creates the activity for a hidden, minimised or unfocused main window.</summary>
    public static AppActivity CreateBackground() => new AppActivity.Background();
}
