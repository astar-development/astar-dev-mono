using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWallhavenUrlBuilder
{
    [Fact]
    public void when_building_a_top_wallpapers_page_url_then_the_page_number_is_appended()
        => WallhavenUrlBuilder.BuildTopWallpapersPageUrl("https://example.test/top/", 3).ShouldBe(new Uri("https://example.test/top/3"));

    [Fact]
    public void when_building_a_hot_wallpapers_page_url_then_the_page_number_is_appended()
        => WallhavenUrlBuilder.BuildHotWallpapersPageUrl("https://example.test/hot/", 3).ShouldBe(new Uri("https://example.test/hot/3"));

    [Fact]
    public void when_building_a_category_page_url_for_page_one_then_only_the_id_placeholder_is_substituted()
    {
        var category = new SearchCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7()), Id = "cat1", Name = "category one" };

        WallhavenUrlBuilder.BuildCategoryPageUrl("https://example.test/search/", string.Empty, category, 1)
            .ShouldBe(new Uri("https://example.test/search/cat1"));
    }

    [Fact]
    public void when_building_a_category_page_url_for_a_later_page_then_the_page_number_is_appended_after_the_substituted_id()
    {
        var category = new SearchCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7()), Id = "cat1", Name = "category one" };

        WallhavenUrlBuilder.BuildCategoryPageUrl("https://example.test/search/", "/", category, 2)
            .ShouldBe(new Uri("https://example.test/search/cat1/2"));
    }

    [Fact]
    public void when_building_a_category_page_url_with_a_suffix_then_the_suffix_follows_the_id_and_precedes_the_page_number()
    {
        var category = new SearchCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7()), Id = "cat1", Name = "category one" };

        WallhavenUrlBuilder.BuildCategoryPageUrl("https://example.test/search/", "&categories=001&page=", category, 3)
            .ShouldBe(new Uri("https://example.test/search/cat1&categories=001&page=3"));
    }

    [Fact]
    public void when_building_a_category_page_url_with_a_suffix_for_page_one_then_the_suffix_is_kept_and_no_page_number_is_appended()
    {
        var category = new SearchCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7()), Id = "cat1", Name = "category one" };

        WallhavenUrlBuilder.BuildCategoryPageUrl("https://example.test/search/", "&categories=001&page=", category, 1)
            .ShouldBe(new Uri("https://example.test/search/cat1&categories=001&page="));
    }

    [Fact]
    public void when_the_template_is_a_relative_path_then_the_built_url_is_a_relative_uri()
    {
        var url = WallhavenUrlBuilder.BuildTopWallpapersPageUrl("api/v1/search?sorting=toplist&page=", 2);

        url.IsAbsoluteUri.ShouldBeFalse();
        url.OriginalString.ShouldBe("api/v1/search?sorting=toplist&page=2");
    }
}
