using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class SyncNotificationPolicy : ISyncNotificationPolicy
{
    /// <inheritdoc />
    public Option<SyncNotification> Decide(Result<SyncPassSummary, SyncPassFailure> outcome, WindowPresence presence) =>
        presence is WindowPresence.InFront ? Option.None<SyncNotification>() : outcome.Match(Describe, Explain);

    private static Option<SyncNotification> Describe(SyncPassSummary summary)
    {
        if (summary.Conflicts > 0)
            return Option.Some(SyncNotificationFactory.CreateConflictsFound(summary.Conflicts));

        if (summary.HeldDeletions > 0)
            return Option.Some(SyncNotificationFactory.CreateDeletionsHeld(summary.HeldDeletions));

        var files = summary.Download.Downloaded + summary.Download.DeletedLocally + summary.Upload.Uploaded + summary.Upload.Removed;

        return files > 0 ? Option.Some(SyncNotificationFactory.CreateSynced(files)) : Option.None<SyncNotification>();
    }

    private static Option<SyncNotification> Explain(SyncPassFailure failure) => failure switch
    {
        SyncPassFailure.Cancelled or SyncPassFailure.AlreadyRunning => Option.None<SyncNotification>(),
        SyncPassFailure.AuthenticationRequired => Option.Some(SyncNotificationFactory.CreateSignInRequired()),
        _ => Option.Some(SyncNotificationFactory.CreateSyncFailed()),
    };
}
