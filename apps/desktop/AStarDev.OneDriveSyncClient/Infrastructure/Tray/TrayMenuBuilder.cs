using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <inheritdoc />
public sealed class TrayMenuBuilder(ILocalizationService loc) : ITrayMenuBuilder
{
    /// <inheritdoc />
    public IReadOnlyList<TrayMenuEntry> Build(IReadOnlyList<TrayAccount> accounts)
    {
        var syncEntries = accounts.Select(account => TrayMenuEntryFactory.CreateSyncNow(account.Id, loc.GetLocal("Tray.SyncNow", account.DisplayName)));

        return
        [
            TrayMenuEntryFactory.CreateOpen(loc.GetLocal("Tray.Open")),
            .. syncEntries,
            TrayMenuEntryFactory.CreateQuit(loc.GetLocal("Tray.Quit"))
        ];
    }
}
