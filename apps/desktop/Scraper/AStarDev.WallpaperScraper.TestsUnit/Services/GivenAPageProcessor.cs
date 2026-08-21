using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Playwright;

namespace AStarDev.WallpaperScraper.TestsUnit.Services;

public sealed class GivenAPageProcessor
{
    [Fact]
    public async Task when_process_page_async_is_called_then_a_localised_status_message_is_reported()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal("Scraper.PageProcessor.Started").Returns("Starting page processing…");
        var page = Substitute.For<IPage>();
        var sut = new PageProcessor(localizationService, page);
        var mockUrl = new Uri("https://example.com");
        var progress = Substitute.For<IProgress<string>>();

        await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting page processing…");
    }

    [Fact]
    public async Task when_process_page_async_fails_then_a_page_failure_is_returned()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var page = Substitute.For<IPage>();
        var sut = new PageProcessor(localizationService, page);
        var mockUrl = new Uri("https://example.com");
        var progress = Substitute.For<IProgress<string>>();

        page.GotoAsync(mockUrl.ToString(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(new MockResponse()
        {
            Status = 404,
            StatusText = "Not Found",
            TextAsyncFunc = () => Task.FromResult<string?>(null)
        }));

        var result = await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<AStar.Dev.FunctionalParadigm.Failure<PageResult>>();

        string statusMessage = ((AStar.Dev.FunctionalParadigm.Failure<PageResult>)result).Exception.Message;
        statusMessage.ShouldBe("Failed to load page content for: https://example.com/ with status message: Not Found");
    }
}
