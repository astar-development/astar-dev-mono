using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging;
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
public sealed class TokenCacheService(IFileSystem fileSystem, ILogger<TokenCacheService> logger) : ITokenCacheService
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

        if (OperatingSystem.IsLinux())
        {
            try
            {
                var keyringProperties = new StorageCreationPropertiesBuilder(
                        $"{ApplicationMetadata.ApplicationNameHyphenated}.bin",
                        CacheDirectory)
                    .WithLinuxKeyring(
                        schemaName: "dev.astar.onedrivesync",
                        collection: MsalCacheHelper.LinuxKeyRingDefaultCollection,
                        secretLabel: "OneDrive Sync token cache",
                        attribute1: new KeyValuePair<string, string>("Version", "1"),
                        attribute2: new KeyValuePair<string, string>("ProductGroup", "AStar"))
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
            catch (Exception ex)
            {
                OneDriveSyncClientMessages.TokenCacheFailed(logger, ex);
            }

            storageProperties = new StorageCreationPropertiesBuilder(
                    $"{ApplicationMetadata.ApplicationNameHyphenated}.plaintext",
                    CacheDirectory)
                .WithLinuxUnprotectedFile()
                .Build();
        }
        else
        {
            storageProperties = new StorageCreationPropertiesBuilder(
                    $"{ApplicationMetadata.ApplicationNameHyphenated}.msalcache",
                    CacheDirectory)
                .WithMacKeyChain(
                    serviceName: ApplicationMetadata.ApplicationNameHyphenated,
                    accountName: "MSALCache")
                .Build();
        }

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);
    }

    private static string InitialiseCacheDirectory(IFileSystem fs)
    {
        string directory = GetPlatformCacheDirectory();
        _ = fs.Directory.CreateDirectory(directory);

        return directory;
    }

    private static string GetPlatformCacheDirectory()
    {
        string appData = Environment.GetFolderPath(
            OperatingSystem.IsWindows()
                ? Environment.SpecialFolder.ApplicationData
                : Environment.SpecialFolder.UserProfile);

        return OperatingSystem.IsWindows()
            ? appData.CombinePath(ApplicationMetadata.ApplicationNameHyphenated)
            : OperatingSystem.IsMacOS()
                ? appData.CombinePath("Library", "Application Support", ApplicationMetadata.ApplicationNameHyphenated)
                : appData.CombinePath(".config", ApplicationMetadata.ApplicationNameHyphenated);
    }
}
