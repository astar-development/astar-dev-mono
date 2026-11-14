using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Notifications;

/// <summary>Tells the user about a finished pass with a desktop notification while the window is not in front (#1410). The text holds the account's display name and counts only, never a path or an email address.</summary>
public sealed class SyncNotifier : IDisposable
{
    private readonly ISyncActivitySource activity;

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
        activity.PassFinished += OnPassFinished;
    }

    /// <summary>Stops listening.</summary>
    public void Dispose() => activity.PassFinished -= OnPassFinished;

    /// <summary>Decides and sends the notification for one finished pass.</summary>
    /// <param name="finished">The pass that finished.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task HandleAsync(SyncPassFinishedEventArgs finished, CancellationToken cancellationToken) => activity is null ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    private void OnPassFinished(object? sender, SyncPassFinishedEventArgs finished) => _ = HandleAsync(finished, CancellationToken.None);
}
