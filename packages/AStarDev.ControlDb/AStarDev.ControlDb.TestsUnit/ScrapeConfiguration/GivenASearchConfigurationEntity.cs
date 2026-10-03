using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsUnit.ScrapeConfiguration;

public sealed class GivenASearchConfigurationEntity
{
    [Fact]
    public void when_the_editable_properties_are_set_then_they_change_and_the_identity_does_not()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var entity = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "old", 10, []);
        var id = entity.Id;

        entity.SearchTerm = "new";
        entity.MaxResults = null;

        entity.SearchTerm.ShouldBe("new");
        entity.MaxResults.ShouldBeNull();
        entity.Id.ShouldBe(id);
        entity.ScrapeConfigurationId.ShouldBe(scrapeConfigurationId);
    }
}
