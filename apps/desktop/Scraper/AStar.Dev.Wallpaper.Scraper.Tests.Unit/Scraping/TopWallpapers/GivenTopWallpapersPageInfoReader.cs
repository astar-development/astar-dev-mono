using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.TopWallpapers;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.TopWallpapers;

public sealed class GivenTopWallpapersPageInfoReader
{
    [Fact]
    public async Task when_the_header_reports_a_page_of_total_then_the_total_page_count_is_returned()
    {
        var page = CreatePageWithHeaderText("Page 1 / 42");
        var sut = new TopWallpapersPageInfoReader();

        int pageCount = await sut.ReadPageCountAsync(page, TestContext.Current.CancellationToken);

        pageCount.ShouldBe(42);
    }

    [Fact]
    public async Task when_the_header_reports_a_single_digit_total_then_it_is_parsed_correctly()
    {
        var page = CreatePageWithHeaderText("Page 3 / 7");
        var sut = new TopWallpapersPageInfoReader();

        int pageCount = await sut.ReadPageCountAsync(page, TestContext.Current.CancellationToken);

        pageCount.ShouldBe(7);
    }

    private static IPage CreatePageWithHeaderText(string headerText)
    {
        var page = Substitute.For<IPage>();
        var header = Substitute.For<ILocator>();
        header.First.Returns(header);
        header.TextContentAsync().Returns(Task.FromResult<string?>(headerText));
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(header);

        return page;
    }
}
