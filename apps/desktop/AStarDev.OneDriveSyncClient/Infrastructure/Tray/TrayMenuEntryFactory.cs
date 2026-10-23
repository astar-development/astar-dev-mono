namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Creates <see cref="TrayMenuEntry"/> instances for each menu entry case.</summary>
public static class TrayMenuEntryFactory
{
    /// <summary>Creates the entry that shows the main window.</summary>
    public static TrayMenuEntry CreateOpen(string label) => new TrayMenuEntry.Open(label);

    /// <summary>Creates the entry that triggers a sync for the given account.</summary>
    public static TrayMenuEntry CreateSyncNow(string accountId, string label) => new TrayMenuEntry.SyncNow(accountId, label);

    /// <summary>Creates the entry that exits the application.</summary>
    public static TrayMenuEntry CreateQuit(string label) => new TrayMenuEntry.Quit(label);
}
