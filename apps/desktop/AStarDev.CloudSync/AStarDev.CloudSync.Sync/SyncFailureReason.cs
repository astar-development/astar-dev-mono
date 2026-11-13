namespace AStarDev.CloudSync.Sync;

/// <summary>Why a sync run for an account failed, as the user sees it (#1402). A cancelled run and a sign-in problem are not failures: they have their own status.</summary>
public abstract record SyncFailureReason
{
    private SyncFailureReason()
    {
    }

    /// <summary>The account's sync root is missing, unmounted or not set.</summary>
    public sealed record RootUnavailable : SyncFailureReason
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The provider is full, so nothing more could be uploaded.</summary>
    public sealed record QuotaExceeded : SyncFailureReason
    {
        internal QuotaExceeded()
        {
        }
    }

    /// <summary>The local volume ran out of space.</summary>
    public sealed record InsufficientDiskSpace : SyncFailureReason
    {
        internal InsufficientDiskSpace()
        {
        }
    }

    /// <summary>Anything else; the details are in the log.</summary>
    public sealed record Unexpected : SyncFailureReason
    {
        internal Unexpected()
        {
        }
    }
}
