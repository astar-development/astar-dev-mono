using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to storage through <see cref="MsalCacheHelper"/>: the Linux keyring, the macOS keychain or Windows DPAPI, or a plaintext file.</summary>
/// <param name="application">The app's one MSAL public client application.</param>
/// <param name="cacheFolder">The folder holding the cache files.</param>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over MsalCacheHelper; the secure store depends on the machine's keyring, so it cannot be tested reliably.")]
public sealed class MsalCacheHelperStorage(IPublicClientApplication application, IDirectoryInfo cacheFolder) : IMsalTokenCacheStorage
{
    private const string SecureCacheFileName = "cloudsync.msalcache";
    private const string PlaintextCacheFileName = "cloudsync.msalcache.plaintext";

    /// <inheritdoc />
    public Task AttachSecureStoreAsync() =>
        RegisterAsync(new StorageCreationPropertiesBuilder(SecureCacheFileName, cacheFolder.FullName)
            .WithLinuxKeyring("dev.astar.cloudsync.tokencache", MsalCacheHelper.LinuxKeyRingDefaultCollection, "AStarDev CloudSync sign-in tokens", new KeyValuePair<string, string>("Version", "1"), new KeyValuePair<string, string>("ProductGroup", "AStarDev.CloudSync"))
            .WithMacKeyChain("AStarDev.CloudSync", "MsalCache")
            .Build());

    /// <inheritdoc />
    public Task AttachPlaintextFileAsync() =>
        RegisterAsync(new StorageCreationPropertiesBuilder(PlaintextCacheFileName, cacheFolder.FullName)
            .WithUnprotectedFile()
            .Build());

    /// <inheritdoc />
    public Task<PlaintextTokenMove> MovePlaintextFileToSecureStoreAsync() => throw new NotImplementedException();

    /// <inheritdoc />
    public Task DetachPlaintextFileAsync() => throw new NotImplementedException();

    private async Task RegisterAsync(StorageCreationProperties properties)
    {
        cacheFolder.Create();
        var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(false);
        helper.VerifyPersistence();
        helper.RegisterCache(application.UserTokenCache);
    }
}
