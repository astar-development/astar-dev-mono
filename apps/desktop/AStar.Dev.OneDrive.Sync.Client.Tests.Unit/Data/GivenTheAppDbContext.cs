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
}
