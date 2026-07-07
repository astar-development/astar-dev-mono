using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenATopWallpapersPage
{
    private static TopWallpapersPage BuildSut(IPage page)
    {
        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));
        var searchConfiguration = new SearchConfigurationBuilder().Build();

        return new TopWallpapersPage(playwrightService, searchConfiguration);
    }

    [Fact]
    public async Task when_loading_the_top_wallpapers_page_for_the_first_time_then_the_result_is_ok()
    {
        var response = Substitute.For<IResponse>();
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(response));
        var sut = BuildSut(page);

        var result = await sut.LoadTopWallpapersPageAsync(1);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_loading_the_top_wallpapers_page_and_navigation_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var sut = BuildSut(page);

        var result = await sut.LoadTopWallpapersPageAsync(1);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_getting_the_page_info_for_the_first_time_then_the_page_count_is_parsed()
    {
        var locator = Substitute.For<ILocator>();
        locator.First.Returns(locator);
        locator.TextContentAsync().Returns(Task.FromResult<string?>("Page 1 / 5"));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(locator);
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Ok<int, ScrapeError>>().Value.ShouldBe(5);
    }

    [Fact]
    public async Task when_the_page_info_header_text_is_missing_then_a_page_parse_failed_error_is_returned()
    {
        var locator = Substitute.For<ILocator>();
        locator.First.Returns(locator);
        locator.TextContentAsync().Returns(Task.FromResult<string?>(null));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(locator);
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Fail<int, ScrapeError>>().Error.ShouldBeOfType<PageParseFailed>();
    }

    [Fact]
    public async Task when_reading_the_page_info_header_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(_ => throw new PlaywrightException("locator failed"));
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Fail<int, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_there_are_no_image_previews_then_the_links_result_is_ok_and_empty()
    {
        var linksLocator = Substitute.For<ILocator>();
        linksLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([]));
        var page = Substitute.For<IPage>();
        page.GetByRole(AriaRole.Link, Arg.Any<PageGetByRoleOptions>()).Returns(linksLocator);
        var sut = BuildSut(page);

        var result = await sut.GetImagePageLinksAsync();

        result.ShouldBeOfType<Ok<IReadOnlyCollection<string>, ScrapeError>>().Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_reading_the_image_previews_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GetByRole(AriaRole.Link, Arg.Any<PageGetByRoleOptions>()).Returns(_ => throw new PlaywrightException("locator failed"));
        var sut = BuildSut(page);

        var result = await sut.GetImagePageLinksAsync();

        result.ShouldBeOfType<Fail<IReadOnlyCollection<string>, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }
}
