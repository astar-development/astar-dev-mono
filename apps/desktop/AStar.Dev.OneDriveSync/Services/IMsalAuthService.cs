namespace AStar.Dev.OneDriveSync.Services;

/// <summary>AM-01: MSAL OAuth for personal Microsoft accounts.</summary>
public interface IMsalAuthService
{
    Task<MsalAuthResult> SignInInteractiveAsync(CancellationToken ct = default);
    Task SignOutAsync(string accountId, CancellationToken ct = default);
    Task<string> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default);
}
