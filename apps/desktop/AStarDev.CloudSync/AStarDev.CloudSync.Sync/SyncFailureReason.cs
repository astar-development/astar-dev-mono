namespace AStarDev.CloudSync.Sync;

/// <summary>Why an account's last sync run did not finish, for the status the user sees (#1402).</summary>
public abstract record SyncFailureReason
{
    private SyncFailureReason()
    {
    }

    /// <summary>The run could not start because the account was busy with other work, or the app is updating or closing.</summary>
    public sealed record CouldNotStart : SyncFailureReason
    {
        internal CouldNotStart()
        {
        }
    }

    /// <summary>The account must sign in again.</summary>
    public sealed record AuthenticationRequired : SyncFailureReason
    {
        internal AuthenticationRequired()
        {
        }
    }

    /// <summary>The account's sync folder is missing or unmounted.</summary>
    public sealed record RootUnavailable : SyncFailureReason
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The provider's storage is full.</summary>
    public sealed record QuotaExceeded : SyncFailureReason
    {
        internal QuotaExceeded()
        {
        }
    }

    /// <summary>The local disk is full.</summary>
    public sealed record InsufficientDiskSpace : SyncFailureReason
    {
        internal InsufficientDiskSpace()
        {
        }
    }

    /// <summary>The user cancelled the run. Shown as cancelled, not as an error.</summary>
    public sealed record Cancelled : SyncFailureReason
    {
        internal Cancelled()
        {
        }
    }

    /// <summary>Anything else.</summary>
    public sealed record Unexpected : SyncFailureReason
    {
        internal Unexpected()
        {
        }
    }
}
