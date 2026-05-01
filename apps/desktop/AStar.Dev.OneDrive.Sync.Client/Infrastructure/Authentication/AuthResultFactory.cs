using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>Creates <see cref="Result{TSuccess,TError}"/> instances for authentication outcomes.</summary>
public static class AuthResultFactory
{
    /// <summary>Returns a cancelled authentication result.</summary>
    public static Result<AuthResult, AuthError> Cancelled() => new Result<AuthResult, AuthError>.Error(new AuthCancelledError());

    /// <summary>Returns a failed authentication result with the given <paramref name="message"/>.</summary>
    public static Result<AuthResult, AuthError> Failure(string message) => new Result<AuthResult, AuthError>.Error(new AuthFailedError(message));

    /// <summary>Returns a successful authentication result containing the token and account details.</summary>
    public static Result<AuthResult, AuthError> Success(string accessToken, string accountId, string displayName, string email)
    {
        ArgumentNullException.ThrowIfNull(accessToken);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(email);

        return new Result<AuthResult, AuthError>.Ok(new AuthResult(accessToken, accountId, displayName, email));
    }
}
