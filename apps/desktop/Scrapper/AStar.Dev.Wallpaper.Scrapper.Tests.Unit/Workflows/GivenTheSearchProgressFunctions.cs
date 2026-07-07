using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenTheSearchProgressFunctions
{
    private const string SearchStringPrefix = "https://example.test/search/";
    private const string SearchStringSuffix = "/?page=";

    private static SearchProgress Progress(SearchConfiguration searchConfiguration)
        => SearchProgressFactory.Create(searchConfiguration, new ScrapeConfigurationBuilder().Build().ScrapeDirectories);

    [Fact]
    public void when_no_category_matches_the_current_search_string_then_the_whole_list_is_returned()
    {
        var categoryOne = new Category { Id = "one", };
        var categoryTwo = new Category { Id = "two", };
        var searchConfiguration = new SearchConfigurationBuilder { SearchString = "no-match", SearchStringPrefix = SearchStringPrefix, SearchStringSuffix = SearchStringSuffix, }.Build();

        var result = SearchProgressFunctions.FilterSearchCategories(searchConfiguration, [categoryOne, categoryTwo,]);

        result.ShouldBe([categoryOne, categoryTwo,]);
    }

    [Fact]
    public void when_a_category_mid_list_matches_the_current_search_string_then_preceding_categories_are_skipped()
    {
        var categoryOne = new Category { Id = "one", };
        var categoryTwo = new Category { Id = "two", };
        var categoryThree = new Category { Id = "three", };
        string matchingSearchString = $"{SearchStringPrefix}two{SearchStringSuffix}";
        var searchConfiguration = new SearchConfigurationBuilder { SearchString = matchingSearchString, SearchStringPrefix = SearchStringPrefix, SearchStringSuffix = SearchStringSuffix, }.Build();

        var result = SearchProgressFunctions.FilterSearchCategories(searchConfiguration, [categoryOne, categoryTwo, categoryThree,]);

        result.ShouldBe([categoryTwo, categoryThree,]);
    }

    [Fact]
    public void when_the_first_category_matches_the_current_search_string_then_the_whole_list_is_returned()
    {
        var categoryOne = new Category { Id = "one", };
        var categoryTwo = new Category { Id = "two", };
        string matchingSearchString = $"{SearchStringPrefix}one{SearchStringSuffix}";
        var searchConfiguration = new SearchConfigurationBuilder { SearchString = matchingSearchString, SearchStringPrefix = SearchStringPrefix, SearchStringSuffix = SearchStringSuffix, }.Build();

        var result = SearchProgressFunctions.FilterSearchCategories(searchConfiguration, [categoryOne, categoryTwo,]);

        result.ShouldBe([categoryOne, categoryTwo,]);
    }

    [Fact]
    public void when_the_combined_search_string_equals_the_current_search_string_then_the_progress_is_unchanged()
    {
        var searchConfiguration = new SearchConfigurationBuilder { SearchString = "same-string", StartingPageNumber = 7, }.Build();
        var progress = Progress(searchConfiguration);

        var result = SearchProgressFunctions.UpdateSearchDetails(progress, "same-string");

        result.SearchConfiguration.StartingPageNumber.ShouldBe(7);
    }

    [Fact]
    public void when_the_combined_search_string_differs_from_the_current_search_string_then_the_search_string_is_updated()
    {
        var searchConfiguration = new SearchConfigurationBuilder { SearchString = "old-string", }.Build();
        var progress = Progress(searchConfiguration);

        var result = SearchProgressFunctions.UpdateSearchDetails(progress, "new-string");

        result.SearchConfiguration.SearchString.ShouldBe("new-string");
    }

    [Fact]
    public void when_the_combined_search_string_differs_from_the_current_search_string_then_the_starting_page_number_is_reset_to_one()
    {
        var searchConfiguration = new SearchConfigurationBuilder { SearchString = "old-string", StartingPageNumber = 9, }.Build();
        var progress = Progress(searchConfiguration);

        var result = SearchProgressFunctions.UpdateSearchDetails(progress, "new-string");

        result.SearchConfiguration.StartingPageNumber.ShouldBe(1);
    }

    [Fact]
    public void when_the_page_count_equals_the_current_total_pages_then_the_progress_is_unchanged()
    {
        var searchConfiguration = new SearchConfigurationBuilder { TotalPages = 3, }.Build();
        var progress = Progress(searchConfiguration);

        var result = SearchProgressFunctions.UpdateTotalPages(progress, 3);

        result.SearchConfiguration.TotalPages.ShouldBe(3);
    }

    [Fact]
    public void when_the_page_count_differs_from_the_current_total_pages_then_the_total_pages_is_updated()
    {
        var searchConfiguration = new SearchConfigurationBuilder { TotalPages = 3, }.Build();
        var progress = Progress(searchConfiguration);

        var result = SearchProgressFunctions.UpdateTotalPages(progress, 8);

        result.SearchConfiguration.TotalPages.ShouldBe(8);
    }

    [Fact]
    public void when_the_sub_directory_name_is_empty_then_the_progress_is_unchanged()
    {
        var scrapeDirectories = new ScrapeConfigurationBuilder().Build().ScrapeDirectories;
        var progress = SearchProgressFactory.Create(new SearchConfigurationBuilder().Build(), scrapeDirectories);

        var result = SearchProgressFunctions.UpdateSubDirectory(progress, string.Empty);

        result.ScrapeDirectories.SubDirectoryName.ShouldBe(scrapeDirectories.SubDirectoryName);
    }

    [Fact]
    public void when_the_sub_directory_name_is_not_empty_then_the_scrape_directories_sub_directory_name_is_updated()
    {
        var scrapeDirectories = new ScrapeConfigurationBuilder().Build().ScrapeDirectories;
        var progress = SearchProgressFactory.Create(new SearchConfigurationBuilder().Build(), scrapeDirectories);

        var result = SearchProgressFunctions.UpdateSubDirectory(progress, "new-sub-directory");

        result.ScrapeDirectories.SubDirectoryName.ShouldBe("new-sub-directory");
    }
}
