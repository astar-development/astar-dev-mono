namespace AStarDev.CloudSync.Accounts;

/// <summary>Why adding an account failed.</summary>
public abstract record AccountAddFailure
{
    private AccountAddFailure()
    {
    }

    /// <summary>The system browser could not be opened for sign-in.</summary>
    public sealed record BrowserUnavailable : AccountAddFailure
    {
        internal BrowserUnavailable()
        {
        }
    }

    /// <summary>The provider rejected the sign-in.</summary>
    public sealed record AuthRejected : AccountAddFailure
    {
        internal AuthRejected()
        {
        }
    }

    /// <summary>The provider could not be reached.</summary>
    public sealed record Network : AccountAddFailure
    {
        internal Network()
        {
        }
    }

    /// <summary>No secure token store was available and the user did not allow the fallback.</summary>
    public sealed record TokenCacheUnavailable : AccountAddFailure
    {
        internal TokenCacheUnavailable()
        {
        }
    }

    /// <summary>The account's drive could not be read.</summary>
    public sealed record DriveUnavailable : AccountAddFailure
    {
        internal DriveUnavailable()
        {
        }
    }

    /// <summary>The account could not be saved.</summary>
    public sealed record PersistFailed : AccountAddFailure
    {
        internal PersistFailed()
        {
        }
    }
}
