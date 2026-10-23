using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <inheritdoc />
public sealed class TrayMenuBuilder(ILocalizationService loc) : ITrayMenuBuilder
{
    /// <inheritdoc />
    public IReadOnlyList<TrayMenuEntry> Build(IReadOnlyList<TrayAccount> accounts) => throw new NotImplementedException(loc.ToString());
}
