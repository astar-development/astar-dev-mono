using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

namespace AStar.Dev.Wallpaper.Scraper.TestsUnit.Scraping.Subscriptions;

public sealed class GivenSubscriptionsClearer
{
    [Fact]
    public async Task when_clearing_all_subscriptions_then_the_clear_all_subscriptions_link_is_clicked()
    {
        var page = Substitute.For<IPage>();
        var divLocator = Substitute.For<ILocator>();
        var filteredLocator = Substitute.For<ILocator>();
        var clearAllLink = Substitute.For<ILocator>();
        page.Locator("div").Returns(divLocator);
        divLocator.Filter(Arg.Any<LocatorFilterOptions>()).Returns(filteredLocator);
        filteredLocator.GetByRole(AriaRole.Link, Arg.Any<LocatorGetByRoleOptions>()).Returns(clearAllLink);
        var sut = new SubscriptionsClearer();

        await sut.ClearAllAsync(page, TestContext.Current.CancellationToken);

        await clearAllLink.Received().ClickAsync();
    }
}
