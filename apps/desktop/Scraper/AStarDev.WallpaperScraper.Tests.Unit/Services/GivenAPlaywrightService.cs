using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.Tests.Unit.Services;

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
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration
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
}
