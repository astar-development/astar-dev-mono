using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class SyncNotificationPolicy : ISyncNotificationPolicy
{
    /// <inheritdoc />
    public Option<SyncNotification> Decide(Result<SyncPassSummary, SyncPassFailure> outcome, WindowPresence presence) => Option.None<SyncNotification>();
}
