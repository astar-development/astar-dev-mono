namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Base type for the entries of the system tray context menu.</summary>
public abstract record TrayMenuEntry
{
    /// <summary>Shows the main window.</summary>
    public sealed record Open(string Label) : TrayMenuEntry;

    /// <summary>Triggers an immediate sync for one account.</summary>
    public sealed record SyncNow(string AccountId, string Label) : TrayMenuEntry;

    /// <summary>Exits the application.</summary>
    public sealed record Quit(string Label) : TrayMenuEntry;
}
