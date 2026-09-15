using System.Reflection;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.Services;

public sealed class GivenAPlaywrightService : IDisposable
{
    private readonly string userDataDirectory = Path.Combine(Path.GetTempPath(), $"playwright-profile-{Guid.NewGuid():N}");
    private readonly MockFileSystem fileSystem = new();

    public void Dispose()
    {
        if (Directory.Exists(userDataDirectory))
            Directory.Delete(userDataDirectory, true);
    }

    private IPlaywrightService CreateSut()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PlaywrightService>();
        var scrapeConfiguration = Options.Create(new ScraperAppConfiguration
        {
            UserDataDirectory = userDataDirectory,
            SearchConfiguration = new SearchConfiguration { BaseUrl = new Uri("https://localhost"), UseHeadless = true },
        });

        return new PlaywrightService(logger, scrapeConfiguration, fileSystem);
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_the_user_data_directory_is_never_created()
    {
        var sut = CreateSut();
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ConfigurePlaywrightAsync(cancellationTokenSource.Token));

        fileSystem.Directory.Exists(userDataDirectory).ShouldBeFalse();
    }

    [Fact]
    public async Task when_stale_chromium_lock_files_exist_then_they_are_removed_while_preparing_the_user_data_directory()
    {
        var sut = CreateSut();
        fileSystem.Directory.CreateDirectory(userDataDirectory);
        fileSystem.File.WriteAllText(Path.Combine(userDataDirectory, "SingletonLock"), string.Empty);
        fileSystem.File.WriteAllText(Path.Combine(userDataDirectory, "SingletonSocket"), string.Empty);
        fileSystem.File.WriteAllText(Path.Combine(userDataDirectory, "SingletonCookie"), string.Empty);

        MethodInfo createUserDataDirectoryAsync = typeof(PlaywrightService).GetMethod("CreateUserDataDirectoryAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)createUserDataDirectoryAsync.Invoke(sut, null)!;

        fileSystem.File.Exists(Path.Combine(userDataDirectory, "SingletonLock")).ShouldBeFalse();
        fileSystem.File.Exists(Path.Combine(userDataDirectory, "SingletonSocket")).ShouldBeFalse();
        fileSystem.File.Exists(Path.Combine(userDataDirectory, "SingletonCookie")).ShouldBeFalse();
    }
}
