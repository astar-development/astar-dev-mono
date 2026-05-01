namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>
/// The success payload returned when authentication succeeds.
/// Use <see cref="AuthResultFactory"/> to obtain a <see cref="AStar.Dev.Functional.Extensions.Result{TSuccess,TError}"/> wrapping this value.
/// </summary>
public sealed record AuthResult(string AccessToken, string AccountId, string DisplayName, string Email);
