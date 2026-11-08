
namespace AStarDev.CloudSync.Accounts;

/// <summary>Creates <see cref="AccountAddFailure"/> cases.</summary>
public static class AccountAddFailureFactory
{
    /// <summary>Creates a browser-unavailable failure.</summary>
    public static AccountAddFailure CreateBrowserUnavailable() => new AccountAddFailure.BrowserUnavailable();

    /// <summary>Creates an auth-rejected failure.</summary>
    public static AccountAddFailure CreateAuthRejected() => new AccountAddFailure.AuthRejected();

    /// <summary>Creates a network failure.</summary>
    public static AccountAddFailure CreateNetwork() => new AccountAddFailure.Network();

    /// <summary>Creates a token-cache-unavailable failure.</summary>
    public static AccountAddFailure CreateTokenCacheUnavailable() => new AccountAddFailure.TokenCacheUnavailable();

    /// <summary>Creates a drive-unavailable failure.</summary>
    public static AccountAddFailure CreateDriveUnavailable() => new AccountAddFailure.DriveUnavailable();

    /// <summary>Creates a persist-failed failure.</summary>
    public static AccountAddFailure CreatePersistFailed() => new AccountAddFailure.PersistFailed();
}
