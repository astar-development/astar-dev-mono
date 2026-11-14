using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Notifications;

/// <summary>Tells the user about a finished pass with a desktop notification while the window is not in front (#1410). The text holds the account's display name and counts only, never a path or an email address.</summary>
public sealed class SyncNotifier : IDisposable
{
    private const string Location = nameof(SyncNotifier);

    private readonly ISyncActivitySource activity;
    private readonly ISyncNotificationPolicy policy;
    private readonly IWindowPresence presence;
    private readonly IAccountStore accounts;
    private readonly INotificationSender sender;
    private readonly ILocalizationService localization;
    private readonly ILogger<SyncNotifier> logger;
    private int warned;

    /// <summary>Starts listening to finished passes.</summary>
    /// <param name="activity">Raises an event when a pass finishes.</param>
    /// <param name="policy">Decides whether a pass is worth a notification.</param>
    /// <param name="presence">Tells whether the window is in front.</param>
    /// <param name="accounts">Supplies the account's display name.</param>
    /// <param name="sender">Shows the notification.</param>
    /// <param name="localization">Supplies the text.</param>
    /// <param name="logger">Reports a sender that failed, once, without details.</param>
    public SyncNotifier(ISyncActivitySource activity, ISyncNotificationPolicy policy, IWindowPresence presence, IAccountStore accounts, INotificationSender sender, ILocalizationService localization, ILogger<SyncNotifier> logger)
    {
        this.activity = activity;
        this.policy = policy;
        this.presence = presence;
        this.accounts = accounts;
        this.sender = sender;
        this.localization = localization;
        this.logger = logger;
        activity.PassFinished += OnPassFinished;
    }

    /// <summary>Stops listening.</summary>
    public void Dispose() => activity.PassFinished -= OnPassFinished;

    /// <summary>Decides and sends the notification for one finished pass. It never throws: a sender that fails is logged once.</summary>
    /// <param name="finished">The pass that finished.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task HandleAsync(SyncPassFinishedEventArgs finished, CancellationToken cancellationToken) =>
        policy.Decide(finished.Outcome, presence.Current).Match(
            notification => Try.RunAsync(() => DeliverAsync(finished.AccountId, notification, cancellationToken), cancellationToken).MatchAsync(done => done, _ => Warn("A desktop notification could not be prepared.")),
            () => Task.CompletedTask);

    private static string BodyKey(SyncNotification notification) => notification switch
    {
        SyncNotification.Synced synced => CountKey.For("Notification.Synced", synced.Files),
        SyncNotification.ConflictsFound conflicts => CountKey.For("Notification.Conflicts", conflicts.Count),
        SyncNotification.DeletionsHeld held => CountKey.For("Notification.Held", held.Count),
        SyncNotification.SignInRequired => "Notification.SignInRequired",
        _ => "Notification.Failed",
    };

    private static int CountOf(SyncNotification notification) => notification switch
    {
        SyncNotification.Synced synced => synced.Files,
        SyncNotification.ConflictsFound conflicts => conflicts.Count,
        SyncNotification.DeletionsHeld held => held.Count,
        _ => 0,
    };

    private void OnPassFinished(object? origin, SyncPassFinishedEventArgs finished) => _ = HandleAsync(finished, CancellationToken.None);

    private Task<Unit> DeliverAsync(AccountId accountId, SyncNotification notification, CancellationToken cancellationToken) =>
        accounts.GetAllAsync(cancellationToken).MatchAsync(
            all => all.FirstOrNone(account => account.Id == accountId).Match(account => SendAsync(account.Registration.DisplayName, notification, cancellationToken), () => Task.FromResult(Unit.Instance)),
            _ => Task.FromResult(Warn("The accounts could not be read, so no desktop notification was shown.")));

    private Task<Unit> SendAsync(string accountName, SyncNotification notification, CancellationToken cancellationToken)
    {
        var body = localization.GetLocal(BodyKey(notification), accountName, CountOf(notification).ToString("N0", localization.CurrentCulture));
        var message = DesktopNotificationFactory.Create(localization.GetLocal("App.Title"), body);

        return sender.SendAsync(message, cancellationToken).MatchAsync(_ => Unit.Instance, _ => WarnOnce());
    }

    private Unit WarnOnce() => Interlocked.Exchange(ref warned, 1) == 0 ? Warn("A desktop notification could not be shown. Further failures are not logged.") : Unit.Instance;

    private Unit Warn(string message)
    {
        LogMessage.Warning(logger, Location, message);

        return Unit.Instance;
    }
}
