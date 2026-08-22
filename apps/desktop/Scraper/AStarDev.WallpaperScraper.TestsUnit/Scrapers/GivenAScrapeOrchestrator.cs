using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Scrapers;
using AStarDev.WallpaperScraper.Services;
using NSubstitute.ReceivedExtensions;

namespace AStarDev.WallpaperScraper.TestsUnit.Scrapers;

public sealed class GivenAScrapeOrchestrator
{
    [Fact]
    public async Task when_scrape_search_categories_async_is_called_then_at_least_one_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Scraping search categories…");
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeSearchCategoriesAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.ReceivedWithAnyArgs(Quantity.AtLeastOne()).Report(default!);
    }

    [Fact]
    public async Task when_scrape_top_async_is_called_then_at_least_one_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Scraping top wallpapers…");
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeTopAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.ReceivedWithAnyArgs(Quantity.AtLeastOne()).Report(default!);
    }

    [Fact]
    public async Task when_scrape_subscribed_async_is_called_then_at_least_one_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns("Scraping subscribed wallpapers…");
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeSubscribedAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.ReceivedWithAnyArgs(Quantity.AtLeastOne()).Report(default!);
    }

    [Fact]
    public async Task when_scrape_search_categories_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        localizationService.GetLocal("Scraper.SearchCategories.Started", Arg.Any<object[]>()).Returns("Starting search categories scrape…");
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeSearchCategoriesAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting search categories scrape…");
    }

    [Fact]
    public async Task when_scrape_subscribed_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        localizationService.GetLocal("Scraper.Subscribed.Started", Arg.Any<object[]>()).Returns("Starting subscribed wallpapers scrape…");
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeSubscribedAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting subscribed wallpapers scrape…");
    }

    [Fact]
    public async Task when_scrape_top_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        localizationService.GetLocal("Scraper.Top.Started", Arg.Any<object[]>()).Returns("Starting top wallpapers scrape…");
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeTopAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting top wallpapers scrape…");
    }

    [Fact]
    public async Task when_scrape_all_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        localizationService.GetLocal("Scraper.All.Started", Arg.Any<object[]>()).Returns("Starting full wallpaper scrape…");
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeAllAsync(progress, mockPage, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting full wallpaper scrape…");
    }

    [Fact]
    public async Task when_scrape_search_categories_async_is_called_then_the_page_processor_is_invoked_with_the_correct_url()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        localizationService.GetLocal("Scraper.SearchCategories.Started", Arg.Any<object[]>()).Returns("Starting search categories scrape…");
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeSearchCategoriesAsync(progress, mockPage, TestContext.Current.CancellationToken);

        await pageProcessor.Received().ProcessPageAsync(Arg.Any<IProgress<string>>(), scrapeConfig.SearchCategoriesUrl, mockPage, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_scrape_subscribed_async_is_called_then_the_page_processor_is_invoked_with_the_correct_url()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var pageProcessor = Substitute.For<IPageProcessor>();
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        localizationService.GetLocal("Scraper.Subscribed.Started", Arg.Any<object[]>()).Returns("Starting subscribed scrape…");
        var sut = new ScrapeOrchestrator(localizationService, pageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeSubscribedAsync(progress, mockPage, TestContext.Current.CancellationToken);

        await pageProcessor.Received().ProcessPageAsync(Arg.Any<IProgress<string>>(), scrapeConfig.SubscribedUrl, mockPage, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_scrape_top_async_is_called_then_the_page_processor_is_invoked_with_the_correct_url()
    {
        var mockLocalizationService = Substitute.For<ILocalizationService>();
        var mockPageProcessor = Substitute.For<IPageProcessor>();
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        mockLocalizationService.GetLocal("Scraper.Top.Started", Arg.Any<object[]>()).Returns("Starting top scrape…");
        var sut = new ScrapeOrchestrator(mockLocalizationService, mockPageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeTopAsync(progress, mockPage, TestContext.Current.CancellationToken);

        await mockPageProcessor.Received().ProcessPageAsync(Arg.Any<IProgress<string>>(), scrapeConfig.TopWallpapersUrl, mockPage, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_scrape_all_async_is_called_then_the_page_processor_is_invoked_with_the_correct_url()
    {
        var mockLocalizationService = Substitute.For<ILocalizationService>();
        var mockPageProcessor = Substitute.For<IPageProcessor>();
        var mockScrapeConfigurationRepository = Substitute.For<IScrapeConfigurationRepository>();
        var mockPage = Substitute.For<Microsoft.Playwright.IPage>();
        var scrapeConfig = new ScrapeConfiguration(new Uri("https://example.com/search-categories"), new Uri("https://example.com/top"), new Uri("https://example.com/subscribed"));
        mockScrapeConfigurationRepository.GetScrapeConfigurationAsync().Returns(scrapeConfig);
        mockLocalizationService.GetLocal("Scraper.All.Started", Arg.Any<object[]>()).Returns("Starting all scrape…");
        var sut = new ScrapeOrchestrator(mockLocalizationService, mockPageProcessor, mockScrapeConfigurationRepository);
        var progress = Substitute.For<IProgress<string>>();

        await sut.ScrapeAllAsync(progress, mockPage, TestContext.Current.CancellationToken);

        await mockPageProcessor.Received().ProcessPageAsync(Arg.Any<IProgress<string>>(), scrapeConfig.SearchCategoriesUrl, mockPage, TestContext.Current.CancellationToken);
        await mockPageProcessor.Received().ProcessPageAsync(Arg.Any<IProgress<string>>(), scrapeConfig.TopWallpapersUrl, mockPage, TestContext.Current.CancellationToken);
        await mockPageProcessor.Received().ProcessPageAsync(Arg.Any<IProgress<string>>(), scrapeConfig.SubscribedUrl, mockPage, TestContext.Current.CancellationToken);
    }
}

