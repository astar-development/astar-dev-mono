using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeDirectoriesEntity
{
    [Fact]
    public void when_the_editable_properties_are_set_then_they_change_and_the_identity_does_not()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var entity = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "old-root", "old-famous", "old-sub");
        var id = entity.Id;

        entity.RootDirectory = "new-root";
        entity.RootDirectoryFamous = "new-famous";
        entity.SubDirectoryName = "new-sub";

        (entity.RootDirectory, entity.RootDirectoryFamous, entity.SubDirectoryName).ShouldBe(("new-root", "new-famous", "new-sub"));
        entity.Id.ShouldBe(id);
        entity.ScrapeConfigurationEntityId.ShouldBe(scrapeConfigurationId);
    }

    [Fact]
    public void when_created_with_values_then_the_constructor_arguments_are_exposed()
    {
        var entity = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), new ScrapeConfigurationId(Guid.CreateVersion7()), "root", "famous", "sub");

        (entity.RootDirectory, entity.RootDirectoryFamous, entity.SubDirectoryName).ShouldBe(("root", "famous", "sub"));
    }
}
