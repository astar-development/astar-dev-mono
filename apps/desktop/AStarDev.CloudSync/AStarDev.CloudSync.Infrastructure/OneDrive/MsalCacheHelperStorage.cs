using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
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
    private Option<MsalCacheHelper> plaintextHelper = Option.None<MsalCacheHelper>();

    private IFileInfo PlaintextFile => cacheFolder.FileSystem.FileInfo.New(cacheFolder.FileSystem.Path.Join(cacheFolder.FullName, PlaintextCacheFileName));

    /// <inheritdoc />
    public async Task AttachSecureStoreAsync() => _ = await RegisterAsync(SecureStoreProperties()).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AttachPlaintextFileAsync() => plaintextHelper = Option.Some(await RegisterAsync(PlaintextFileProperties()).ConfigureAwait(false));

    /// <inheritdoc />
    public async Task<PlaintextTokenMove> MovePlaintextFileToSecureStoreAsync()
    {
        if (!PlaintextFile.Exists)
            return PlaintextTokenMoveFactory.CreateNothingToMove();

        var source = await MsalCacheHelper.CreateAsync(PlaintextFileProperties()).ConfigureAwait(false);
        var target = await MsalCacheHelper.CreateAsync(SecureStoreProperties()).ConfigureAwait(false);
        target.SaveUnencryptedTokenCache(source.LoadUnencryptedTokenCache());
        PlaintextFile.Delete();

        return PlaintextTokenMoveFactory.CreateMoved();
    }

    /// <inheritdoc />
    public async Task DetachPlaintextFileAsync()
    {
        await plaintextHelper.Match(ForgetPlaintextAccountsAsync, () => Task.CompletedTask).ConfigureAwait(false);
        plaintextHelper = Option.None<MsalCacheHelper>();
        PlaintextFile.Delete();
    }

    private async Task ForgetPlaintextAccountsAsync(MsalCacheHelper helper)
    {
        helper.UnregisterCache(application.UserTokenCache);

        foreach (var account in await application.GetAccountsAsync().ConfigureAwait(false))
            await application.RemoveAsync(account).ConfigureAwait(false);
    }

    private StorageCreationProperties SecureStoreProperties() =>
        new StorageCreationPropertiesBuilder(SecureCacheFileName, cacheFolder.FullName)
            .WithLinuxKeyring("dev.astar.cloudsync.tokencache", MsalCacheHelper.LinuxKeyRingDefaultCollection, "AStarDev CloudSync sign-in tokens", new KeyValuePair<string, string>("Version", "1"), new KeyValuePair<string, string>("ProductGroup", "AStarDev.CloudSync"))
            .WithMacKeyChain("AStarDev.CloudSync", "MsalCache")
            .Build();

    private StorageCreationProperties PlaintextFileProperties() =>
        new StorageCreationPropertiesBuilder(PlaintextCacheFileName, cacheFolder.FullName)
            .WithUnprotectedFile()
            .Build();

    private async Task<MsalCacheHelper> RegisterAsync(StorageCreationProperties properties)
    {
        cacheFolder.Create();
        var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(false);
        helper.VerifyPersistence();
        helper.RegisterCache(application.UserTokenCache);

        return helper;
    }
}
