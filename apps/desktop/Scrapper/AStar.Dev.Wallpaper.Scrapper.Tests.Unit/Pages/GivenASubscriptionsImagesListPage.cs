using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenASubscriptionsImagesListPage
{
    private static SubscriptionsImagesListPage BuildSut(IPage page)
    {
        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));
        var searchConfiguration = new SearchConfigurationBuilder().Build();

        return new SubscriptionsImagesListPage(playwrightService, searchConfiguration);
    }

    [Fact]
    public async Task when_loading_the_subscriptions_page_succeeds_then_the_result_is_ok()
    {
        var response = Substitute.For<IResponse>();
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>()).Returns(Task.FromResult<IResponse?>(response));
        var sut = BuildSut(page);

        var result = await sut.LoadSubscriptionResultsPageAsync(1);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_loading_the_subscriptions_page_and_navigation_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var sut = BuildSut(page);

        var result = await sut.LoadSubscriptionResultsPageAsync(1);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_the_header_reports_new_subscription_wallpapers_then_the_page_info_is_parsed_successfully()
    {
        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>("24 New Subscription Wallpapers"));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);
        var sut = BuildSut(page);

        var result = await sut.PageInfoAsync();

        result.ShouldBeOfType<Ok<PageInfo, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_subscriptions_header_text_is_missing_then_a_page_parse_failed_error_is_returned()
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
    public async Task when_reading_the_subscriptions_header_throws_then_a_page_load_failed_error_is_returned()
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
        page.GetByRole(AriaRole.Link).Returns(linksLocator);
        var sut = BuildSut(page);

        var result = await sut.GetImagePageLinksAsync();

        result.ShouldBeOfType<Ok<IReadOnlyCollection<string>, ScrapeError>>().Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_reading_the_image_previews_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GetByRole(AriaRole.Link).Returns(_ => throw new PlaywrightException("locator failed"));
        var sut = BuildSut(page);

        var result = await sut.GetImagePageLinksAsync();

        result.ShouldBeOfType<Fail<IReadOnlyCollection<string>, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_clearing_subscriptions_succeeds_then_the_result_is_ok()
    {
        var clearLink = Substitute.For<ILocator>();
        clearLink.ClickAsync().Returns(Task.CompletedTask);
        var filteredLocator = Substitute.For<ILocator>();
        filteredLocator.GetByRole(AriaRole.Link, Arg.Any<LocatorGetByRoleOptions>()).Returns(clearLink);
        var divLocator = Substitute.For<ILocator>();
        divLocator.Filter(Arg.Any<LocatorFilterOptions>()).Returns(filteredLocator);
        var page = Substitute.For<IPage>();
        page.Locator("div").Returns(divLocator);
        var sut = BuildSut(page);

        var result = await sut.ClearAsync();

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_clearing_subscriptions_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.Locator("div").Returns(_ => throw new PlaywrightException("locator failed"));
        var sut = BuildSut(page);

        var result = await sut.ClearAsync();

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }
}
