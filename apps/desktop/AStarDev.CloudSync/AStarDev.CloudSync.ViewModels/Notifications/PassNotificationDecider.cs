using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Notifications;

/// <summary>Decides whether a finished pass is worth a notification, given where the window is now.</summary>
/// <param name="policy">Decides from the outcome and the window.</param>
/// <param name="presence">Tells whether the window is in front.</param>
public sealed class PassNotificationDecider(ISyncNotificationPolicy policy, IWindowPresence presence)
{
    /// <summary>Decides for one finished pass.</summary>
    /// <param name="outcome">How the pass ended.</param>
    public Option<SyncNotification> Decide(Result<SyncPassSummary, SyncPassFailure> outcome) => policy.Decide(outcome, presence.Current);
}
