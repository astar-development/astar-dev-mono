namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Base type for whether the user is currently looking at the application.</summary>
public abstract record AppActivity
{
    /// <summary>The main window is visible and has focus.</summary>
    public sealed record Foreground : AppActivity;

    /// <summary>The main window is hidden, minimised or unfocused.</summary>
    public sealed record Background : AppActivity;
}
