namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="NotificationFailure"/> cases.</summary>
public static class NotificationFailureFactory
{
    /// <summary>Creates the <see cref="NotificationFailure.NotSupported"/> case.</summary>
    public static NotificationFailure CreateNotSupported() => new NotificationFailure.NotSupported();

    /// <summary>Creates the <see cref="NotificationFailure.SenderFailed"/> case.</summary>
    public static NotificationFailure CreateSenderFailed() => new NotificationFailure.SenderFailed();
}
