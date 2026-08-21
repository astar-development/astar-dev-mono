using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Services;

namespace AStarDev.WallpaperScraper.TestsUnit.Services;

public sealed class GivenAPageProcessor
{
    [Fact]
    public async Task when_process_page_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.PageProcessor.Started").Returns("Starting page processing…");
        var playwrightService = Substitute.For<IPlaywrightService>();
        var sut = new PageProcessor(localizationService, playwrightService);
        var mockUrl = new Uri("https://example.com");
        var progress = Substitute.For<IProgress<string>>();

        await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting page processing…");
    }
}
