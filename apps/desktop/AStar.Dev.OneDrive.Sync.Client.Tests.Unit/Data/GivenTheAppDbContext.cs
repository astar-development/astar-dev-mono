using AStar.Dev.OneDrive.Sync.Client.Data;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data;

public sealed class GivenTheAppDbContext
{
    [Fact]
    public void when_inspected_then_file_classification_rules_db_set_does_not_exist() =>
        typeof(AppDbContext).GetProperty("FileClassificationRules").ShouldBeNull();

    [Fact]
    public void when_inspected_then_synced_item_file_classifications_db_set_exists() =>
        typeof(AppDbContext).GetProperty("SyncedItemFileClassifications").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_files_db_set_exists() =>
        typeof(AppDbContext).GetProperty("Files").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_file_access_details_db_set_exists() =>
        typeof(AppDbContext).GetProperty("FileAccessDetails").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_file_name_parts_db_set_exists() =>
        typeof(AppDbContext).GetProperty("FileNameParts").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_tags_to_ignore_db_set_exists() =>
        typeof(AppDbContext).GetProperty("TagsToIgnore").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_models_to_ignore_db_set_exists() =>
        typeof(AppDbContext).GetProperty("ModelsToIgnore").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_scrape_configuration_db_set_exists() =>
        typeof(AppDbContext).GetProperty("ScrapeConfiguration").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_search_configurations_db_set_exists() =>
        typeof(AppDbContext).GetProperty("SearchConfigurations").ShouldNotBeNull();

    [Fact]
    public void when_inspected_then_scraped_tags_db_set_exists() =>
        typeof(AppDbContext).GetProperty("ScrapedTags").ShouldNotBeNull();
}
