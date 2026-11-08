using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Connects the MSAL token cache to storage through <see cref="MsalCacheHelper"/>: the Linux keyring, the macOS keychain or Windows DPAPI, or a plaintext file.</summary>
/// <param name="application">The app's one MSAL public client application.</param>
/// <param name="cacheFolder">The folder holding the cache files.</param>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over MsalCacheHelper; the secure store depends on the machine's keyring, so it cannot be tested reliably.")]
internal sealed class MsalCacheHelperStorage(IPublicClientApplication application, IDirectoryInfo cacheFolder) : IMsalTokenCacheStorage
{
    private const string SecureCacheFileName = "cloudsync.msalcache";
    private const string PlaintextCacheFileName = "cloudsync.msalcache.plaintext";
    private readonly Lock gate = new();
    private Option<MsalCacheHelper> secureHelper = Option.None<MsalCacheHelper>();
    private Option<MsalCacheHelper> plaintextHelper = Option.None<MsalCacheHelper>();

    private IFileInfo PlaintextFile => cacheFolder.FileSystem.FileInfo.New(cacheFolder.FileSystem.Path.Join(cacheFolder.FullName, PlaintextCacheFileName));

    /// <inheritdoc />
    public async Task AttachSecureStoreAsync()
    {
        var helper = await CreateVerifiedAsync(SecureStoreProperties()).ConfigureAwait(false);

        lock (gate)
            secureHelper = RegisterOnce(secureHelper, helper);
    }

    /// <inheritdoc />
    public async Task AttachPlaintextFileAsync()
    {
        var helper = await CreateVerifiedAsync(PlaintextFileProperties()).ConfigureAwait(false);

        lock (gate)
            plaintextHelper = RegisterOnce(plaintextHelper, helper);
    }

    /// <inheritdoc />
    public async Task<PlaintextTokenMove> MovePlaintextFileToSecureStoreAsync()
    {
        if (!PlaintextFile.Exists)
            return PlaintextTokenMoveFactory.CreateNothingToMove();

        var source = await MsalCacheHelper.CreateAsync(PlaintextFileProperties()).ConfigureAwait(false);
        var target = await MsalCacheHelper.CreateAsync(SecureStoreProperties()).ConfigureAwait(false);
        target.SaveUnencryptedTokenCache(MsalCacheBlobMerger.Merge(target.LoadUnencryptedTokenCache(), source.LoadUnencryptedTokenCache()));
        PlaintextFile.Delete();

        return PlaintextTokenMoveFactory.CreateMoved();
    }

    /// <inheritdoc />
    public async Task DetachPlaintextFileAsync()
    {
        Option<MsalCacheHelper> detached;

        lock (gate)
        {
            detached = plaintextHelper;
            plaintextHelper = Option.None<MsalCacheHelper>();
            _ = detached.Match(Unregister, () => Unit.Instance);
        }

        await detached.Match(_ => ForgetInMemoryAccountsAsync(), () => Task.CompletedTask).ConfigureAwait(false);
        PlaintextFile.Delete();
    }

    private Unit Unregister(MsalCacheHelper helper)
    {
        helper.UnregisterCache(application.UserTokenCache);

        return Unit.Instance;
    }

    private Option<MsalCacheHelper> RegisterOnce(Option<MsalCacheHelper> registered, MsalCacheHelper helper) =>
        registered.Match(Option.Some, () =>
        {
            helper.RegisterCache(application.UserTokenCache);

            return Option.Some(helper);
        });

    private async Task ForgetInMemoryAccountsAsync()
    {
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

    private async Task<MsalCacheHelper> CreateVerifiedAsync(StorageCreationProperties properties)
    {
        cacheFolder.Create();
        var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(false);
        helper.VerifyPersistence();

        return helper;
    }
}
