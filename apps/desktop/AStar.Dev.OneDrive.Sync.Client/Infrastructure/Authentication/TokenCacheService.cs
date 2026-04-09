using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>
/// Wires MSAL's cross-platform token cache persistence using
/// Microsoft.Identity.Client.Extensions.Msal.
///
/// Cache location is platform-appropriate:
///   Linux   — ~/.config/AStar.Dev.OneDrive.Sync/
///   Windows — %AppData%\AStar.Dev.OneDrive.Sync\
///   macOS   — ~/Library/Application Support/AStar.Dev.OneDrive.Sync/
///
/// The cache file is encrypted using the platform keychain where available
/// (libsecret on Linux, DPAPI on Windows, Keychain on macOS).
/// Falls back to plaintext with a warning on unsupported platforms.
/// </summary>
public sealed class TokenCacheService : ITokenCacheService
{
    private const int KeyringTimeoutSeconds = 5;
    private const string CacheFileName = "msal_token_cache.bin";
    private const string AppName       = "AStar.Dev.OneDrive.Sync";

    public TokenCacheService()
    {
        CacheDirectory = GetPlatformCacheDirectory();
        _ = Directory.CreateDirectory(CacheDirectory);
    }

    /// <summary>
    /// Registers the file-backed cache with the given
    /// <see cref="IPublicClientApplication"/> instance.
    /// Must be called once after the app is built.
    /// </summary>
    public async Task RegisterAsync(IPublicClientApplication app)
    {
        StorageCreationProperties storageProperties;

        if(OperatingSystem.IsLinux())
        {
            try
            {
                var keyringProperties = new StorageCreationPropertiesBuilder(
                        CacheFileName,
                        CacheDirectory)
                    .WithLinuxKeyring(
                        schemaName:  "dev.astar.onedrivesync",
                        collection:  MsalCacheHelper.LinuxKeyRingDefaultCollection,
                        secretLabel: "OneDrive Sync token cache",
                        attribute1:  new KeyValuePair<string, string>("Version", "1"),
                        attribute2:  new KeyValuePair<string, string>("ProductGroup", "AStar"))
                    .WithMacKeyChain(
                        serviceName: AppName,
                        accountName: "MSALCache")
                    .Build();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(KeyringTimeoutSeconds));
                var helper = await MsalCacheHelper
                    .CreateAsync(keyringProperties)
                    .WaitAsync(cts.Token);
                helper.RegisterCache(app.UserTokenCache);
                return;
            }
            catch(Exception ex)
            {
                Serilog.Log.Warning(ex,
                    "[TokenCache] Keyring unavailable, falling back to plaintext cache");
            }

            // Fallback — separate builder with only unprotected file
            storageProperties = new StorageCreationPropertiesBuilder(
                    CacheFileName + ".plaintext",
                    CacheDirectory)
                .WithLinuxUnprotectedFile()
                .Build();
        }
        else
        {
            // Windows / macOS — use keychain/DPAPI
            storageProperties = new StorageCreationPropertiesBuilder(
                    CacheFileName,
                    CacheDirectory)
                .WithMacKeyChain(
                    serviceName: AppName,
                    accountName: "MSALCache")
                .Build();
        }

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);
    }

    public string CacheDirectory { get; }

    private static string GetPlatformCacheDirectory()
    {
        // Use the OS-appropriate application data folder
        string appData = Environment.GetFolderPath(
            OperatingSystem.IsWindows()
                ? Environment.SpecialFolder.ApplicationData
                : Environment.SpecialFolder.UserProfile);

        return OperatingSystem.IsWindows()
            ? Path.Combine(appData, AppName)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(appData, "Library", "Application Support", AppName)
                : Path.Combine(appData, ".config", AppName);
    }
}
