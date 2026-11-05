using AStarDev.CloudSync.Core.Accounts;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Calls MSAL to sign in through the system browser and manage cached accounts.</summary>
/// <param name="application">The app's one MSAL public client application.</param>
/// <param name="options">The scopes to request.</param>
public sealed class MsalClient(IPublicClientApplication application, IOptions<MicrosoftAccountOptions> options) : IMsalClient
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetAccountKeysAsync() => throw new NotImplementedException($"{application}{options}");

    /// <inheritdoc />
    public Task<MicrosoftSignIn> SignInAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task RemoveAccountAsync(ProviderAccountKey key) => throw new NotImplementedException();
}
