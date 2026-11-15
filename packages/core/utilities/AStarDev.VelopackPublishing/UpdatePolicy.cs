namespace AStar.Dev.Velopack.Publishing;

/// <inheritdoc />
public sealed class UpdatePolicy : IUpdatePolicy
{
    private static readonly TimeSpan RemindEvery = TimeSpan.FromHours(1);
    private static readonly TimeSpan ForceAfter = TimeSpan.FromDays(7);

    /// <inheritdoc />
    public UpdateAction Decide(UpdateCheckResult result, UpdateDeferral deferral, DateTimeOffset now)
    {
        if (result is not UpdateFound found)
            return UpdateActionFactory.CreateNone();

        if (deferral is not DeferredSince deferred || deferred.Version != found.Info.TargetFullRelease.Version.ToString())
            return UpdateActionFactory.CreatePrompt();

        if (now - deferred.FirstDeferredAt >= ForceAfter)
            return UpdateActionFactory.CreateForceAfterWarning();

        var remindAt = deferred.LastPromptedAt + RemindEvery;

        return now >= remindAt ? UpdateActionFactory.CreatePrompt() : UpdateActionFactory.CreateRemindLater(remindAt);
    }
}
