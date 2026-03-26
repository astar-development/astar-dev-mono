using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.old.Services;

/// <summary>AM-01: MSAL OAuth for personal Microsoft accounts.</summary>
public interface IMsalAuthService
{
    Task<Result<MsalAuthResult, string>> SignInInteractiveAsync(CancellationToken ct = default);
    Task SignOutAsync(string accountId, CancellationToken ct = default);
    Task<Result<string, string>> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default);
}
