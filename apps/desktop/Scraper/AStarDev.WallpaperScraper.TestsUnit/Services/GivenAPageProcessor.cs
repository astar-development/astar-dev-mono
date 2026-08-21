using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Playwright;
using NSubstitute.ExceptionExtensions;

namespace AStarDev.WallpaperScraper.TestsUnit.Services;

public sealed class GivenAPageProcessor
{
    [Fact]
    public async Task when_process_page_async_is_called_then_a_localised_status_message_is_reported()
    {
        var (localizationService, sut, mockUrl, progress, _) = CreateSut();
        localizationService.GetLocal("Scraper.PageProcessor.Started").Returns("Starting page processing…");

        await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        progress.Received().Report("Starting page processing…");
    }

    [Fact]
    public async Task when_process_page_async_fails_then_a_page_failure_is_returned()
    {
        var (localizationService, sut, mockUrl, progress, page) = CreateSut();
        localizationService.GetLocal("Scraper.PageProcessor.FailedToLoad", Arg.Any<object[]>()).Returns(callInfo => string.Format("Failed to load page content for: {0} with status message: {1}.", callInfo.ArgAt<object[]>(1)));

        page.GotoAsync(mockUrl.ToString(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(new MockResponse()
        {
            Status = 404,
            StatusText = "Not Found",
            TextAsyncFunc = () => Task.FromResult<string?>(null)
        }));

        var result = await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<PageResult>>();

        string statusMessage = ((Failure<PageResult>)result).Exception.Message;
        statusMessage.ShouldBe("Failed to load page content for: https://example.com/ with status message: Not Found.");
    }

    [Fact]
    public async Task when_process_page_async_succeeds_then_a_page_success_is_returned()
    {
        var (localizationService, sut, mockUrl, progress, page) = CreateSut();

        page.GotoAsync(mockUrl.ToString(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(new MockResponse()
        {
            Status = 200,
            StatusText = "OK",
            TextAsyncFunc = () => Task.FromResult<string?>("<html>Mock page content</html>")
        }));

        var result = await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<PageResult>>();

        var pageSuccess = (PageSuccess)((Success<PageResult>)result).Value;
        pageSuccess.Content.ShouldBe("<html>Mock page content</html>");
        pageSuccess.Url.ShouldBe(mockUrl);
    }

    [Fact]
    public async Task when_process_page_async_throws_an_exception_then_a_page_failure_is_returned()
    {
        var (_, sut, mockUrl, progress, page) = CreateSut();

        page.GotoAsync(mockUrl.ToString(), Arg.Any<PageGotoOptions>()).ThrowsAsync(new OperationException("Simulated exception"));

        var result = await sut.ProcessPageAsync(progress, mockUrl, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<PageResult>>();

        string statusMessage = ((Failure<PageResult>)result).Exception.Message;
        statusMessage.ShouldBe("Simulated exception");
    }

    private static (ILocalizationService localizationService, PageProcessor sut, Uri mockUrl, IProgress<string> progress, IPage page) CreateSut()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var page = Substitute.For<IPage>();
        var sut = new PageProcessor(localizationService, page);
        var mockUrl = new Uri("https://example.com");
        var progress = Substitute.For<IProgress<string>>();

        return (localizationService, sut, mockUrl, progress, page);
    }
}
