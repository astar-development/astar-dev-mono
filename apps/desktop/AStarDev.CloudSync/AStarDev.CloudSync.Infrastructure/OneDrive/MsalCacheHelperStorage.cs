using System.IO.Abstractions;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to storage through <c>MsalCacheHelper</c>.</summary>
/// <param name="application">The app's one MSAL public client application.</param>
/// <param name="cacheFolder">The folder holding the cache files.</param>
public sealed class MsalCacheHelperStorage(IPublicClientApplication application, IDirectoryInfo cacheFolder) : IMsalTokenCacheStorage
{
    /// <inheritdoc />
    public Task AttachSecureStoreAsync() => throw new NotImplementedException($"{application}{cacheFolder}");

    /// <inheritdoc />
    public Task AttachPlaintextFileAsync() => throw new NotImplementedException();
}
