using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Scrapers;
using NSubstitute.ReceivedExtensions;

namespace AStarDev.WallpaperScraper.TestsUnit.Scrapers;

public sealed class GivenAScrapeOrchestrator
{
    [Fact]
    public async Task when_scrape_search_categories_async_is_called_then_at_least_one_status_message_is_reported_before_it_throws()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Scraping search categories…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeSearchCategoriesAsync(progress, TestContext.Current.CancellationToken));

        progress.ReceivedWithAnyArgs(Quantity.AtLeastOne()).Report(default!);
    }

    [Fact]
    public async Task when_scrape_top_async_is_called_then_at_least_one_status_message_is_reported_before_it_throws()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Scraping top wallpapers…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeTopAsync(progress, TestContext.Current.CancellationToken));

        progress.ReceivedWithAnyArgs(Quantity.AtLeastOne()).Report(default!);
    }

    [Fact]
    public async Task when_scrape_subscribed_async_is_called_then_at_least_one_status_message_is_reported_before_it_throws()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Scraping subscribed wallpapers…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeSubscribedAsync(progress, TestContext.Current.CancellationToken));

        progress.ReceivedWithAnyArgs(Quantity.AtLeastOne()).Report(default!);
    }

    [Fact]
    public async Task when_scrape_all_async_is_called_then_a_localised_status_message_is_reported_before_it_throws()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.All.Started").Returns("Starting full wallpaper scrape…");
        var sut = new ScrapeOrchestrator(localizationService);
        var progress = Substitute.For<IProgress<string>>();

        await Should.ThrowAsync<NotImplementedException>(() => sut.ScrapeAllAsync(progress, TestContext.Current.CancellationToken));

        progress.Received().Report("Starting full wallpaper scrape…");
    }
}
