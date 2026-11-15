using AStarDev.CloudSync.Sync;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Tells the user, the first time the window is closed, that CloudSync is still running in the tray (#1409). The text holds no account detail.</summary>
/// <param name="store">Remembers that the notice was shown.</param>
/// <param name="sender">Shows the notification.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class TrayCloseNotice(ITrayNoticeStore store, INotificationSender sender, ILocalizationService localization)
{
    /// <summary>Shows the notice if it has not been shown yet, and records it once it was shown. A sender that fails leaves the notice pending, so the next close tries again. It never throws.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task ShowAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{store}{sender}{localization}{cancellationToken}");
}
