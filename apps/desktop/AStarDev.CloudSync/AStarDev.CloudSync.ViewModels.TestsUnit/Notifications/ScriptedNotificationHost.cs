using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Notifications;

internal sealed class ScriptedNotificationHost : ISyncNotificationPolicy, IWindowPresence, INotificationSender
{
    private readonly List<DesktopNotification> sent = [];

    public Option<SyncNotification> Decision { get; set; } = Option.None<SyncNotification>();

    public WindowPresence Current { get; set; } = WindowPresenceFactory.CreateNotInFront();

    public Result<Unit, NotificationFailure> Outcome { get; set; } = Result.Success<Unit, NotificationFailure>(Unit.Instance);

    public WindowPresence? PresenceAsked { get; private set; }

    public IReadOnlyList<DesktopNotification> Sent => sent;

    public Option<SyncNotification> Decide(Result<SyncPassSummary, SyncPassFailure> outcome, WindowPresence presence)
    {
        PresenceAsked = presence;

        return Decision;
    }

    public Task<Result<Unit, NotificationFailure>> SendAsync(DesktopNotification notification, CancellationToken cancellationToken)
    {
        sent.Add(notification);

        return Task.FromResult(Outcome);
    }
}
