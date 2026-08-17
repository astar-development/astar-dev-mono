using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Authentication;

/// <summary>Creates <see cref="Result{TSuccess,TError}"/> instances for authentication outcomes.</summary>
public static class AuthResultFactory
{
    /// <summary>Returns a cancelled authentication result.</summary>
    public static Result<AuthResult, AuthError> Cancelled() => new Fail<AuthResult, AuthError>(new AuthCancelledError());

    /// <summary>Returns a failed authentication result with the given <paramref name="message"/>.</summary>
    public static Result<AuthResult, AuthError> Failure(string message) => new Fail<AuthResult, AuthError>(new AuthFailedError(message));

    /// <summary>Returns a re-authentication-required result with the MSAL <paramref name="errorCode"/> and <paramref name="classification"/>.</summary>
    public static Result<AuthResult, AuthError> ReAuthRequired(string errorCode, string classification) => new Fail<AuthResult, AuthError>(new AuthReAuthRequiredError(errorCode, classification));

    /// <summary>Returns a successful authentication result containing the token and account details. Token expiry defaults to <see cref="DateTimeOffset.MaxValue"/> (does not expire).</summary>
    public static Result<AuthResult, AuthError> Success(string accessToken, string accountId, AccountProfile profile)
        => Success(accessToken, accountId, profile, DateTimeOffset.MaxValue);

    /// <summary>Returns a successful authentication result containing the token, account details, and token expiry.</summary>
    public static Result<AuthResult, AuthError> Success(string accessToken, string accountId, AccountProfile profile, DateTimeOffset expiresOn)
        => new Ok<AuthResult, AuthError>(new AuthResult(accessToken, accountId, profile, expiresOn));
}
