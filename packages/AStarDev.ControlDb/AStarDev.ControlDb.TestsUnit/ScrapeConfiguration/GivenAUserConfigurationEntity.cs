using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsUnit.ScrapeConfiguration;

public sealed class GivenAUserConfigurationEntity
{
    [Fact]
    public void when_the_editable_properties_are_set_then_they_change_and_the_identity_does_not()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var entity = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "old@example.test", "old", "old-key");
        var id = entity.Id;

        entity.EmailAddress = "new@example.test";
        entity.Username = "new";
        entity.ApiKey = "new-key";

        entity.EmailAddress.ShouldBe("new@example.test");
        entity.Username.ShouldBe("new");
        entity.ApiKey.ShouldBe("new-key");
        entity.Id.ShouldBe(id);
        entity.ScrapeConfigurationEntityId.ShouldBe(scrapeConfigurationId);
    }

    [Fact]
    public void when_created_with_values_then_the_constructor_arguments_are_exposed()
    {
        var entity = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), new ScrapeConfigurationId(Guid.CreateVersion7()), "user@example.test", "user", "key");

        (entity.EmailAddress, entity.Username, entity.ApiKey).ShouldBe(("user@example.test", "user", "key"));
    }
}
