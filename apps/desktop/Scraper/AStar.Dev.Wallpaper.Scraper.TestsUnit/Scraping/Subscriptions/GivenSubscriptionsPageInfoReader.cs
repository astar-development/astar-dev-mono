using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

namespace AStar.Dev.Wallpaper.Scraper.TestsUnit.Scraping.Subscriptions;

public sealed class GivenSubscriptionsPageInfoReader
{
    [Fact]
    public async Task when_the_header_reports_an_image_count_then_the_page_count_is_derived_from_it()
    {
        var page = CreatePageWithHeaderText("50 New Subscription Wallpapers");
        var sut = new SubscriptionsPageInfoReader();

        var pageInfo = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        pageInfo.ImageCount.ShouldBe(50);
        pageInfo.PageCount.ShouldBe(3);
    }

    [Fact]
    public async Task when_the_header_reports_a_comma_separated_image_count_then_the_commas_are_ignored()
    {
        var page = CreatePageWithHeaderText("1,234 New Subscription Wallpapers");
        var sut = new SubscriptionsPageInfoReader();

        var pageInfo = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        pageInfo.ImageCount.ShouldBe(1_234);
        pageInfo.PageCount.ShouldBe(52);
    }

    [Fact]
    public async Task when_the_image_count_evenly_divides_the_page_size_then_the_page_count_is_exact()
    {
        var page = CreatePageWithHeaderText("48 New Subscription Wallpapers");
        var sut = new SubscriptionsPageInfoReader();

        var pageInfo = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        pageInfo.PageCount.ShouldBe(2);
    }

    private static IPage CreatePageWithHeaderText(string headerText)
    {
        var page = Substitute.For<IPage>();
        var header = Substitute.For<ILocator>();
        header.TextContentAsync().Returns(Task.FromResult<string?>(headerText));
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(header);

        return page;
    }
}
