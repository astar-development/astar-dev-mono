using System.Text.Json;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationImportMapper
{
    [Fact]
    public void when_the_document_has_no_max_results_then_the_entity_has_no_max_results() =>
        DocumentWith(null).ToEntity(DateTimeOffset.UnixEpoch).SearchConfiguration.MaxResults.ShouldBeNull();

    [Fact]
    public void when_the_document_has_zero_max_results_then_the_entity_keeps_zero() =>
        DocumentWith(0).ToEntity(DateTimeOffset.UnixEpoch).SearchConfiguration.MaxResults.ShouldBe(0);

    [Fact]
    public void when_the_document_has_max_results_then_the_entity_keeps_it() =>
        DocumentWith(25).ToEntity(DateTimeOffset.UnixEpoch).SearchConfiguration.MaxResults.ShouldBe(25);

    [Fact]
    public void when_a_configuration_with_hot_wallpapers_is_exported_and_imported_then_the_hot_settings_are_kept()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration();
        configuration.HotWallpapers = "hot/";
        configuration.HotWallpapersStartingPageNumber = 5;
        configuration.HotWallpapersTotalPages = 55;

        var imported = configuration.ToImportDocument(ApiKeyExport.Exclude).ToEntity(DateTimeOffset.UnixEpoch);

        (imported.HotWallpapers, imported.HotWallpapersStartingPageNumber, imported.HotWallpapersTotalPages).ShouldBe(("hot/", 5, 55));
    }

    [Fact]
    public void when_an_exported_file_has_no_hot_wallpapers_settings_then_the_url_defaults_and_the_pages_are_zero()
    {
        var document = JsonSerializer.Deserialize<ScrapeConfigurationImportDocument>("{}")!;

        var imported = document.ToEntity(DateTimeOffset.UnixEpoch);

        (imported.HotWallpapers, imported.HotWallpapersStartingPageNumber, imported.HotWallpapersTotalPages).ShouldBe(("HotWallpapers", 0, 0));
    }

    [Fact]
    public void when_a_document_is_mapped_then_the_categories_are_created_and_updated_at_the_given_time()
    {
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var document = new ScrapeConfigurationImportDocument
        {
            SearchConfiguration = new SearchConfigurationImportDocument
            {
                SearchCategories = [new SearchCategoryImportDocument { Id = "100", Name = "Cats" }],
                PersonCategories = ["Models"]
            }
        };

        var search = document.ToEntity(now).SearchConfiguration;

        var stamps = search.SearchCategories.Select(category => (category.CreatedAt, category.UpdatedAt)).Concat(search.PersonCategories.Select(category => (category.CreatedAt, category.UpdatedAt)));
        stamps.ShouldBe([(now, now), (now, now)]);
    }

    private static ScrapeConfigurationImportDocument DocumentWith(int? maxResults) =>
        new() { SearchConfiguration = new SearchConfigurationImportDocument { MaxResults = maxResults } };
}
