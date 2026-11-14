using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>The sender for platforms that have none yet (#1410): every call says it is not supported.</summary>
[ExcludeFromCodeCoverage(Justification = "A constant answer.")]
internal sealed class UnsupportedNotificationSender : INotificationSender
{
    /// <inheritdoc />
    public Task<Result<Unit, NotificationFailure>> SendAsync(DesktopNotification notification, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<Unit, NotificationFailure>(NotificationFailureFactory.CreateNotSupported()));
}
