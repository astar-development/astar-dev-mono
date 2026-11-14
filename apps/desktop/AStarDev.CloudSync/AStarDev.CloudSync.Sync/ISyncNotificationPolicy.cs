using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides whether a finished pass is worth a desktop notification and what it says (#1410).</summary>
public interface ISyncNotificationPolicy
{
    /// <summary>Decides from the pass's outcome and where the window is. Nothing is sent while the window is in front, for a pass that changed nothing, or for a pass that was cancelled or skipped.</summary>
    /// <param name="outcome">How the pass ended.</param>
    /// <param name="presence">Whether the user is looking at the window.</param>
    Option<SyncNotification> Decide(Result<SyncPassSummary, SyncPassFailure> outcome, WindowPresence presence);
}
