using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Scrapers;

namespace AStarDev.WallpaperScraper.TestsUnit.Scrapers;

public sealed class GivenAScrapeOrchestrator
{
    [Fact]
    public async Task when_scrape_search_categories_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.SearchCategories.Started").Returns("Starting search categories scrape…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeSearchCategoriesAsync(progress, TestContext.Current.CancellationToken));

        progress.Received().Report("Starting search categories scrape…");
    }

    [Fact]
    public async Task when_scrape_top_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.Top.Started").Returns("Starting top wallpapers scrape…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeTopAsync(progress, TestContext.Current.CancellationToken));

        progress.Received().Report("Starting top wallpapers scrape…");
    }

    [Fact]
    public async Task when_scrape_subscribed_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.Subscribed.Started").Returns("Starting subscribed wallpapers scrape…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeSubscribedAsync(progress, TestContext.Current.CancellationToken));

        progress.Received().Report("Starting subscribed wallpapers scrape…");
    }

    [Fact]
    public async Task when_scrape_all_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.All.Started").Returns("Starting full wallpaper scrape…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeAllAsync(progress, TestContext.Current.CancellationToken));

        progress.Received().Report("Starting full wallpaper scrape…");
    }
}
