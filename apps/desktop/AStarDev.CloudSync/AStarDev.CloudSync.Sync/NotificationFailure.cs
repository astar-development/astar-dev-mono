namespace AStarDev.CloudSync.Sync;

/// <summary>Why a notification was not shown (#1410).</summary>
public abstract record NotificationFailure
{
    private NotificationFailure()
    {
    }

    /// <summary>This platform has no sender.</summary>
    public sealed record NotSupported : NotificationFailure
    {
        internal NotSupported()
        {
        }
    }

    /// <summary>The sender ran and failed, for example because the notification program is missing.</summary>
    public sealed record SenderFailed : NotificationFailure
    {
        internal SenderFailed()
        {
        }
    }
}
