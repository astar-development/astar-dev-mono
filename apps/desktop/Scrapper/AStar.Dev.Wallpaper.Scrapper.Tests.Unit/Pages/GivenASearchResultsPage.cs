using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenASearchResultsPage
{
    private const string SearchString = "https://example.test/search/cars/?page=";
    private const int PageNumber = 3;

    private static SearchResultsPage BuildSut(IPage page)
    {
        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        return new SearchResultsPage(playwrightService);
    }

    private static IPage BuildPageReturning(IResponse? response)
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult(response));

        return page;
    }

    [Fact]
    public async Task when_the_first_navigation_attempt_succeeds_then_the_result_is_ok()
    {
        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);
        var page = BuildPageReturning(okResponse);
        var sut = BuildSut(page);

        var result = await sut.LoadSearchPageAsync(SearchString, PageNumber);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_first_navigation_attempt_succeeds_then_goto_async_is_called_exactly_once()
    {
        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);
        var page = BuildPageReturning(okResponse);
        var sut = BuildSut(page);

        await sut.LoadSearchPageAsync(SearchString, PageNumber);

        await page.Received(1).GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>());
    }

    [Fact]
    public async Task when_both_navigation_attempts_report_not_ok_then_goto_async_is_called_exactly_twice()
    {
        var notOkResponse = Substitute.For<IResponse>();
        notOkResponse.Ok.Returns(false);
        var page = BuildPageReturning(notOkResponse);
        var sut = BuildSut(page);

        await sut.LoadSearchPageAsync(SearchString, PageNumber);

        await page.Received(2).GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>());
    }

    [Fact]
    public async Task when_both_navigation_attempts_report_not_ok_then_a_page_load_failed_error_is_returned()
    {
        var notOkResponse = Substitute.For<IResponse>();
        notOkResponse.Ok.Returns(false);
        var page = BuildPageReturning(notOkResponse);
        var sut = BuildSut(page);

        var result = await sut.LoadSearchPageAsync(SearchString, PageNumber);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_both_navigation_attempts_report_not_ok_then_the_page_load_failed_error_carries_the_full_url()
    {
        var notOkResponse = Substitute.For<IResponse>();
        notOkResponse.Ok.Returns(false);
        var page = BuildPageReturning(notOkResponse);
        var sut = BuildSut(page);

        var result = await sut.LoadSearchPageAsync(SearchString, PageNumber);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>().Url.ShouldBe($"{SearchString}{PageNumber}");
    }

    [Fact]
    public async Task when_navigation_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var sut = BuildSut(page);

        var result = await sut.LoadSearchPageAsync(SearchString, PageNumber);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_the_header_reports_a_wallpaper_count_then_the_page_info_is_parsed_successfully()
    {
        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>("48 Wallpapers found for #Cars"));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Is<string>(text => text.Contains("Wallpapers found")), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Ok<PageInfo, ScrapeError>>().Value.SubDirectoryName.ShouldBe("Cars");
    }

    [Fact]
    public async Task when_the_header_text_is_missing_then_a_page_parse_failed_error_is_returned()
    {
        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>(null));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Fail<PageInfo, ScrapeError>>().Error.ShouldBeOfType<PageParseFailed>();
    }

    [Fact]
    public async Task when_reading_the_header_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(_ => throw new PlaywrightException("locator failed"));
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Fail<PageInfo, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_there_are_no_image_previews_then_the_links_result_is_ok_and_empty()
    {
        var linksLocator = Substitute.For<ILocator>();
        linksLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([]));
        var page = Substitute.For<IPage>();
        page.GetByRole(AriaRole.Link, Arg.Any<PageGetByRoleOptions>()).Returns(linksLocator);
        var sut = BuildSut(page);

        var result = await sut.ImagePageLinksAsync();

        result.ShouldBeOfType<Ok<IReadOnlyCollection<string>, ScrapeError>>().Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_reading_the_image_previews_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GetByRole(AriaRole.Link, Arg.Any<PageGetByRoleOptions>()).Returns(_ => throw new PlaywrightException("locator failed"));
        var sut = BuildSut(page);

        var result = await sut.ImagePageLinksAsync();

        result.ShouldBeOfType<Fail<IReadOnlyCollection<string>, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }
}
