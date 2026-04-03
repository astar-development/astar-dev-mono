using Microsoft.Identity.Client;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     MSAL client implementation (AU-01).
///     Handles system browser OAuth flow for consumers tenant only.
/// </summary>
internal sealed class MsalClient(IPublicClientApplication msalApp) : IMsalClient
{
    private readonly IPublicClientApplication _msalApp = msalApp ?? throw new ArgumentNullException(nameof(msalApp));

    public async Task<Result<(AccessToken, string?), string>> AcquireTokenInteractiveAsync(CancellationToken ct = default)
    {
        string[] scopes = ["Files.ReadWrite", "offline_access"];

        try
        {
            var result = await _msalApp
                .AcquireTokenInteractive(scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(ct)
                .ConfigureAwait(false);

            var accessToken = new AccessToken(result.AccessToken, result.ExpiresOn);
            // MSAL does not expose RefreshToken in AuthenticationResult
            string? refreshToken = null;

            return new Result<(AccessToken, string?), string>.Ok((accessToken, refreshToken));
        }
        catch (MsalClientException ex)
        {
            return new Result<(AccessToken, string?), string>.Error($"Interactive auth failed: {ex.Message}");
        }
        catch (MsalServiceException ex)
        {
            return new Result<(AccessToken, string?), string>.Error($"Interactive auth service error: {ex.Message}");
        }
    }

    public async Task<Result<AccessToken, string>> AcquireTokenSilentAsync(CancellationToken ct = default)
    {
        string[] scopes = ["Files.ReadWrite", "offline_access"];
        var accounts = await _msalApp.GetAccountsAsync().ConfigureAwait(false);

        if (!accounts.Any())
            return new Result<AccessToken, string>.Error("No cached tokens; user must re-authenticate");

        try
        {
            var result = await _msalApp
                .AcquireTokenSilent(scopes, accounts.First())
                .ExecuteAsync(ct)
                .ConfigureAwait(false);

            var accessToken = new AccessToken(result.AccessToken, result.ExpiresOn);
            return new Result<AccessToken, string>.Ok(accessToken);
        }
        catch (MsalUiRequiredException)
        {
            return new Result<AccessToken, string>.Error("User interaction required; token expired");
        }
        catch (MsalClientException ex)
        {
            return new Result<AccessToken, string>.Error($"Silent token acquisition failed: {ex.Message}");
        }
        catch (MsalServiceException ex)
        {
            return new Result<AccessToken, string>.Error($"Silent token service error: {ex.Message}");
        }
    }
}

