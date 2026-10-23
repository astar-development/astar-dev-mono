namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Builds the ordered entries of the system tray context menu.</summary>
public interface ITrayMenuBuilder
{
    /// <summary>Returns Open, one Sync Now entry per account, then Quit.</summary>
    IReadOnlyList<TrayMenuEntry> Build(IReadOnlyList<TrayAccount> accounts);
}
