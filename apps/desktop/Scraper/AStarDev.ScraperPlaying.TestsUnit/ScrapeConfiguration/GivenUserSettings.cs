using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenUserSettings
{
    [Fact]
    public void when_applied_to_an_entity_then_the_user_values_are_replaced_and_the_identity_and_other_sections_are_untouched()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        var userConfigurationId = entity.UserConfiguration.Id;

        new UserSettings("new@example.test", "new-user", "new-key").ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.UserConfiguration.EmailAddress.ShouldBe("new@example.test");
        entity.UserConfiguration.Username.ShouldBe("new-user");
        entity.UserConfiguration.ApiKey.ShouldBe("new-key");
        entity.UserConfiguration.Id.ShouldBe(userConfigurationId);
        entity.UserConfiguration.ScrapeConfigurationEntityId.ShouldBe(entity.Id);
        entity.SearchConfiguration.SearchTerm.ShouldBe("term");
        entity.ScrapeDirectories.RootDirectory.ShouldBe("root");
    }

    [Fact]
    public void when_created_from_an_entity_then_the_values_are_copied() =>
        UserSettings.From(GivenAUserSettingsInput.CreateEntity()).ShouldBe(new UserSettings("user@example.test", "user", "api-key"));
}
