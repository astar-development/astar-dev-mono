using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Notifications;

/// <summary>Tells the user about a finished pass with a desktop notification while the window is not in front (#1410). The text holds the account's display name and counts only, never a path or an email address. Passes that finish within <see cref="CombineWindow"/> of each other are told about in one notification (decision 2).</summary>
public sealed class SyncNotifier : IDisposable
{
    private const string Location = nameof(SyncNotifier);

    private readonly ISyncActivitySource activity;
    private readonly PassNotificationDecider decider;
    private readonly IAccountStore accounts;
    private readonly INotificationSender sender;
    private readonly ILocalizationService localization;
    private readonly ILogger<SyncNotifier> logger;
    private readonly TimeProvider time;
    private readonly object gate = new();
    private Dictionary<AccountId, int> pending = [];
    private Task? flush;
    private int warned;

    /// <summary>Starts listening to finished passes.</summary>
    /// <param name="activity">Raises an event when a pass finishes.</param>
    /// <param name="decider">Decides whether a pass is worth a notification.</param>
    /// <param name="accounts">Supplies the account's display name.</param>
    /// <param name="sender">Shows the notification.</param>
    /// <param name="localization">Supplies the text.</param>
    /// <param name="logger">Reports a sender that failed, once, without details.</param>
    /// <param name="time">Times the window in which finished passes are combined.</param>
    public SyncNotifier(ISyncActivitySource activity, PassNotificationDecider decider, IAccountStore accounts, INotificationSender sender, ILocalizationService localization, ILogger<SyncNotifier> logger, TimeProvider time)
    {
        this.activity = activity;
        this.decider = decider;
        this.accounts = accounts;
        this.sender = sender;
        this.localization = localization;
        this.logger = logger;
        this.time = time;
        activity.PassFinished += OnPassFinished;
    }

    /// <summary>How long a finished pass waits for other accounts to finish, so that several are told about as one.</summary>
    public static TimeSpan CombineWindow { get; } = TimeSpan.FromSeconds(5);

    /// <summary>Stops listening.</summary>
    public void Dispose() => activity.PassFinished -= OnPassFinished;

    /// <summary>Decides and sends the notification for one finished pass. It never throws: a sender that fails is logged once.</summary>
    /// <param name="finished">The pass that finished.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task HandleAsync(SyncPassFinishedEventArgs finished, CancellationToken cancellationToken) =>
        decider.Decide(finished.Outcome).Match(
            notification => notification is SyncNotification.Synced synced ? Combine(finished.AccountId, synced.Files) : Try.RunAsync(() => DeliverAsync(finished.AccountId, notification, cancellationToken), cancellationToken).MatchAsync(done => done, _ => Warn("A desktop notification could not be prepared.")),
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

    private Task Combine(AccountId accountId, int files)
    {
        lock (gate)
        {
            pending[accountId] = pending.GetValueOrDefault(accountId) + files;

            return flush ??= FlushAsync();
        }
    }

    private async Task FlushAsync()
    {
        await Task.Delay(CombineWindow, time, CancellationToken.None).ConfigureAwait(false);
        Dictionary<AccountId, int> batch;
        lock (gate)
        {
            batch = pending;
            pending = [];
            flush = null;
        }

        _ = await Try.RunAsync(() => SendBatchAsync(batch, CancellationToken.None), CancellationToken.None).MatchAsync(done => done, _ => Warn("A desktop notification could not be prepared.")).ConfigureAwait(false);
    }

    private Task<Unit> SendBatchAsync(Dictionary<AccountId, int> batch, CancellationToken cancellationToken) =>
        accounts.GetAllAsync(cancellationToken).MatchAsync(
            all => SendCombinedAsync([.. all.Where(account => batch.ContainsKey(account.Id)).Select(account => (account.Registration.DisplayName, Files: batch[account.Id]))], cancellationToken),
            _ => Task.FromResult(Warn("The accounts could not be read, so no desktop notification was shown.")));

    private Task<Unit> SendCombinedAsync(IReadOnlyList<(string Name, int Files)> told, CancellationToken cancellationToken) => told.Count switch
    {
        0 => Task.FromResult(Unit.Instance),
        1 => SendAsync(told[0].Name, SyncNotificationFactory.CreateSynced(told[0].Files), cancellationToken),
        _ => SendTextAsync(localization.GetLocal("Notification.SyncedAccounts", told.Count.ToString("N0", localization.CurrentCulture), told.Sum(account => account.Files).ToString("N0", localization.CurrentCulture)), cancellationToken),
    };

    private Task<Unit> DeliverAsync(AccountId accountId, SyncNotification notification, CancellationToken cancellationToken) =>
        accounts.GetAllAsync(cancellationToken).MatchAsync(
            all => all.FirstOrNone(account => account.Id == accountId).Match(account => SendAsync(account.Registration.DisplayName, notification, cancellationToken), () => Task.FromResult(Unit.Instance)),
            _ => Task.FromResult(Warn("The accounts could not be read, so no desktop notification was shown.")));

    private Task<Unit> SendAsync(string accountName, SyncNotification notification, CancellationToken cancellationToken)
    {
        var body = localization.GetLocal(BodyKey(notification), accountName, CountOf(notification).ToString("N0", localization.CurrentCulture));

        return SendTextAsync(body, cancellationToken);
    }

    private Task<Unit> SendTextAsync(string body, CancellationToken cancellationToken)
    {
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
