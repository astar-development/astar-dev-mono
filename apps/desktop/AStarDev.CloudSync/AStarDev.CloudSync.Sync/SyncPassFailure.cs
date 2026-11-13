namespace AStarDev.CloudSync.Sync;

/// <summary>Why a whole sync run for an account did not finish (#1402). One file failing never fails the run.</summary>
public abstract record SyncPassFailure
{
    private SyncPassFailure()
    {
    }

    /// <summary>A run for the account is already in progress, or no run may start because an update or shutdown is draining the lock. Nothing was started.</summary>
    public sealed record AlreadyRunning : SyncPassFailure
    {
        internal AlreadyRunning()
        {
        }
    }

    /// <summary>The account must sign in again. Its sign-in state is set to sign-in required.</summary>
    public sealed record AuthenticationRequired : SyncPassFailure
    {
        internal AuthenticationRequired()
        {
        }
    }

    /// <summary>The account's sync root is missing, unmounted or not set, so nothing was changed.</summary>
    public sealed record RootUnavailable : SyncPassFailure
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The provider is full. The downloads of this run already ran; no more uploads were sent.</summary>
    public sealed record QuotaExceeded : SyncPassFailure
    {
        internal QuotaExceeded()
        {
        }
    }

    /// <summary>The local volume ran out of space. The whole run stopped: no upload pass followed.</summary>
    public sealed record InsufficientDiskSpace : SyncPassFailure
    {
        internal InsufficientDiskSpace()
        {
        }
    }

    /// <summary>The run was cancelled. Work already finished stays recorded.</summary>
    public sealed record Cancelled : SyncPassFailure
    {
        internal Cancelled()
        {
        }
    }

    /// <summary>Anything else.</summary>
    public sealed record Unexpected : SyncPassFailure
    {
        internal Unexpected()
        {
        }
    }
}
