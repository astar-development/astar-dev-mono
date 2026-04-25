using Microsoft.Identity.Client;

namespace AnotherOneDriveSync.Core;

public interface IAuthService
{
    Task<AuthenticationResult> AcquireTokenAsync();
    Task<AuthenticationResult> AcquireTokenSilentAsync();
    Task<AuthenticationResult> AcquireTokenInteractiveAsync();
    Task SignOutAsync();
}
