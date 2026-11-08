namespace AStarDev.CloudSync.Accounts;

/// <summary>Why adding an account failed.</summary>
public abstract record AccountAddFailure
{
    private AccountAddFailure()
    {
    }

    /// <summary>The system browser could not be opened for sign-in.</summary>
    public sealed record BrowserUnavailable : AccountAddFailure;

    /// <summary>The provider rejected the sign-in.</summary>
    public sealed record AuthRejected : AccountAddFailure;

    /// <summary>The provider could not be reached.</summary>
    public sealed record Network : AccountAddFailure;

    /// <summary>No secure token store was available and the user did not allow the fallback.</summary>
    public sealed record TokenCacheUnavailable : AccountAddFailure;

    /// <summary>The account's drive could not be read.</summary>
    public sealed record DriveUnavailable : AccountAddFailure;

    /// <summary>The account could not be saved.</summary>
    public sealed record PersistFailed : AccountAddFailure;
}
