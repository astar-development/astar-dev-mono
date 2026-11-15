using System.Runtime.Versioning;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenAPlaintextTokenFile : IDisposable
{
    private const string FileName = "cloudsync.msalcache.plaintext";

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"cloudsync-token-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public async Task when_the_plaintext_cache_is_saved_then_only_the_owner_can_read_and_write_the_file()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows has no Unix file modes.");
        var helper = await CreateHelperAsync();

        helper.SaveUnencryptedTokenCache([1, 2, 3]);

        File.GetUnixFileMode(Path.Combine(folder, FileName)).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public async Task when_the_plaintext_cache_is_saved_again_then_the_file_stays_owner_only()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows has no Unix file modes.");
        var helper = await CreateHelperAsync();
        helper.SaveUnencryptedTokenCache([1, 2, 3]);

        helper.SaveUnencryptedTokenCache([4, 5, 6, 7]);

        File.GetUnixFileMode(Path.Combine(folder, FileName)).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private async Task<MsalCacheHelper> CreateHelperAsync()
    {
        _ = Directory.CreateDirectory(folder);
        var properties = new StorageCreationPropertiesBuilder(FileName, folder).WithUnprotectedFile().Build();
        var helper = await MsalCacheHelper.CreateAsync(properties);
        helper.VerifyPersistence();

        return helper;
    }
}
