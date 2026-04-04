using System;
using System.Reactive.Subjects;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>
///     In-process toast notification service (S012, SE-15).
///     Publishes non-blocking toast events via a hot observable that the Dashboard view subscribes to.
/// </summary>
internal sealed class AvaloniaToastService : IToastService, IDisposable
{
    private readonly Subject<ToastNotification> _notifications = new();

    /// <summary>Hot observable of pending toast notifications — subscribe in the view layer.</summary>
    public IObservable<ToastNotification> Notifications => _notifications;

    /// <inheritdoc />
    public void Show(string message, string accountId)
        => _notifications.OnNext(new ToastNotification(message, accountId));

    /// <inheritdoc />
    public void Dispose() => _notifications.Dispose();
}

/// <summary>Payload for a toast notification (SE-15).</summary>
public sealed record ToastNotification(string Message, string AccountId);
