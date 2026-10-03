using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsUnit.ScrapeConfiguration;

public sealed class GivenASearchCategoryEntity
{
    [Fact]
    public void when_the_scrape_progress_is_recorded_then_the_image_count_last_page_and_total_pages_are_updated()
    {
        var category = new SearchCategoryEntity { Id = "1", Name = "Nature", LastKnownImageCount = 1, LastPageVisited = 1, TotalPages = 1 };

        category.RecordScrapeProgress(lastKnownImageCount: 50, lastPageVisited: 4, totalPages: 10);

        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages).ShouldBe((50, 4, 10));
    }
}
