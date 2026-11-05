using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="AccountAddFailure"/> cases.</summary>
public static class AccountAddFailureFactory
{
    /// <summary>Creates a browser-unavailable failure.</summary>
    public static AccountAddFailure CreateBrowserUnavailable() => throw new NotImplementedException();

    /// <summary>Creates an auth-rejected failure.</summary>
    public static AccountAddFailure CreateAuthRejected() => throw new NotImplementedException();

    /// <summary>Creates a network failure.</summary>
    public static AccountAddFailure CreateNetwork() => throw new NotImplementedException();

    /// <summary>Creates a token-cache-unavailable failure.</summary>
    public static AccountAddFailure CreateTokenCacheUnavailable() => throw new NotImplementedException();

    /// <summary>Creates a drive-unavailable failure.</summary>
    public static AccountAddFailure CreateDriveUnavailable() => throw new NotImplementedException();

    /// <summary>Creates a persist-failed failure.</summary>
    public static AccountAddFailure CreatePersistFailed() => throw new NotImplementedException();
}
