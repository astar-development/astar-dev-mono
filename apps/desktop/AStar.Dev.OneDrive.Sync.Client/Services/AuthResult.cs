namespace AStar.Dev.OneDrive.Sync.Client.Services.Auth;

/// <summary>
/// Outcome of an authentication operation.
/// Use the static factory methods rather than constructing directly.
/// </summary>
public sealed class AuthResult
{
    public bool IsSuccess { get; private init; }
    public bool IsCancelled { get; private init; }
    public bool IsError => !IsSuccess && !IsCancelled;
    public string? AccessToken { get; private init; }
    public string? AccountId { get; private init; }
    public string? DisplayName { get; private init; }
    public string? Email { get; private init; }
    public string? ErrorMessage { get; private init; }

    private AuthResult() { }

    public static AuthResult Success(
        string accessToken,
        string accountId,
        string displayName,
        string email) => new()
        {
            IsSuccess = true,
            AccessToken = accessToken,
            AccountId = accountId,
            DisplayName = displayName,
            Email = email
        };

    public static AuthResult Cancelled() => new()
    {
        IsCancelled = true
    };

    public static AuthResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
