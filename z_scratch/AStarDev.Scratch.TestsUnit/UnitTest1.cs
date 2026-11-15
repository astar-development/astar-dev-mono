using AStarDev.Scratch.TestsUnit.ScrapeServices.Models;

namespace AStarDev.Scratch.TestsUnit;

public class SearchSiteShould
{
    [Fact]
    public void contain_the_siteName_of_type_siteName() => new SearchSite(new("Mock Site Name")).SiteName.Value.ShouldBe("Mock Site Name");
}
