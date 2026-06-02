using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>Creates <see cref="Result{TSuccess,TError}"/> instances for authentication outcomes.</summary>
public static class AuthResultFactory
{
    /// <summary>Returns a cancelled authentication result.</summary>
    public static Result<AuthResult, AuthError> Cancelled() => new Result<AuthResult, AuthError>.Error(new AuthCancelledError());

    /// <summary>Returns a failed authentication result with the given <paramref name="message"/>.</summary>
    public static Result<AuthResult, AuthError> Failure(string message) => new Result<AuthResult, AuthError>.Error(new AuthFailedError(message));

    /// <summary>Returns a re-authentication-required result with the MSAL <paramref name="errorCode"/> and <paramref name="classification"/>.</summary>
    public static Result<AuthResult, AuthError> ReAuthRequired(string errorCode, string classification) => new Result<AuthResult, AuthError>.Error(new AuthReAuthRequiredError(errorCode, classification));

    /// <summary>Returns a successful authentication result containing the token and account details. Token expiry defaults to <see cref="DateTimeOffset.MaxValue"/> (does not expire).</summary>
    public static Result<AuthResult, AuthError> Success(string accessToken, string accountId, AccountProfile profile)
        => Success(accessToken, accountId, profile, DateTimeOffset.MaxValue);

    /// <summary>Returns a successful authentication result containing the token, account details, and token expiry.</summary>
    public static Result<AuthResult, AuthError> Success(string accessToken, string accountId, AccountProfile profile, DateTimeOffset expiresOn)
        => new Result<AuthResult, AuthError>.Ok(new AuthResult(accessToken, accountId, profile, expiresOn));
}
