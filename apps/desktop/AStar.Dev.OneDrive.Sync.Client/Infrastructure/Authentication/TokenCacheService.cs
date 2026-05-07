using System.IO.Abstractions;
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
public sealed class TokenCacheService(IFileSystem fileSystem) : ITokenCacheService
{
    private const int KeyringTimeoutSeconds = 5;

    public string CacheDirectory { get; } = InitialiseCacheDirectory(fileSystem);

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
                        $"{ApplicationMetadata.ApplicationNameLowered}.bin",
                        CacheDirectory)
                    .WithLinuxKeyring(
                        schemaName:  "dev.astar.onedrivesync",
                        collection:  MsalCacheHelper.LinuxKeyRingDefaultCollection,
                        secretLabel: "OneDrive Sync token cache",
                        attribute1:  new KeyValuePair<string, string>("Version", "1"),
                        attribute2:  new KeyValuePair<string, string>("ProductGroup", "AStar"))
                    .WithMacKeyChain(
                        serviceName: ApplicationMetadata.ApplicationName,
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

            storageProperties = new StorageCreationPropertiesBuilder(
                    $"{ApplicationMetadata.ApplicationNameLowered}.plaintext",
                    CacheDirectory)
                .WithLinuxUnprotectedFile()
                .Build();
        }
        else
        {
            storageProperties = new StorageCreationPropertiesBuilder(
                    $"{ApplicationMetadata.ApplicationNameLowered}.msalcache",
                    CacheDirectory)
                .WithMacKeyChain(
                    serviceName: ApplicationMetadata.ApplicationNameLowered,
                    accountName: "MSALCache")
                .Build();
        }

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);
    }

    private static string InitialiseCacheDirectory(IFileSystem fs)
    {
        string directory = GetPlatformCacheDirectory(fs);
        _ = fs.Directory.CreateDirectory(directory);

        return directory;
    }

    private static string GetPlatformCacheDirectory(IFileSystem fs)
    {
        string appData = Environment.GetFolderPath(
            OperatingSystem.IsWindows()
                ? Environment.SpecialFolder.ApplicationData
                : Environment.SpecialFolder.UserProfile);

        return OperatingSystem.IsWindows()
            ? fs.Path.Combine(appData, ApplicationMetadata.ApplicationNameLowered)
            : OperatingSystem.IsMacOS()
                ? fs.Path.Combine(appData, "Library", "Application Support", ApplicationMetadata.ApplicationNameLowered)
                : fs.Path.Combine(appData, ".config", ApplicationMetadata.ApplicationNameLowered);
    }
}
