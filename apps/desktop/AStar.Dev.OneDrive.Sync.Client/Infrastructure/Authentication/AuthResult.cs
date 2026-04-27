namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>
/// Outcome of an authentication operation.
/// Use the static factory methods rather than constructing directly.
/// </summary>
public sealed record AuthResult(bool IsSuccess, bool IsCancelled, string? AccessToken, string? AccountId, string? DisplayName, string? Email, string? ErrorMessage);
