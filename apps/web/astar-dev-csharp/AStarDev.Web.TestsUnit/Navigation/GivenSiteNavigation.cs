using AStarDev.Web.Navigation;

namespace AStarDev.Web.TestsUnit.Navigation;

public class GivenSiteNavigation
{
    [Fact]
    public void when_links_are_read_then_the_five_primary_pages_are_present_in_order()
    {
        SiteNavigation.Links.Select(l => l.Href).ShouldBe(["/", "/packages", "/blog", "/case-studies", "/contact"]);
    }

    [Theory]
    [InlineData("/", "/", true)]
    [InlineData("/", "/packages", false)]
    [InlineData("/packages", "/packages", true)]
    [InlineData("/packages", "/packages/", true)]
    [InlineData("/packages", "/", false)]
    [InlineData("/case-studies", "/case-studies/some-slug", true)]
    [InlineData("/case-studies", "/case-studies-other", false)]
    public void when_checking_if_a_link_is_active_then_the_correct_result_is_returned(string href, string currentPath, bool expected)
    {
        SiteNavigation.IsActive(href, currentPath).ShouldBe(expected);
    }
}
