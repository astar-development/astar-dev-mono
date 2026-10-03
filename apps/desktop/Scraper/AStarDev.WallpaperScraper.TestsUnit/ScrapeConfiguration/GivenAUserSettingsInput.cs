using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenAUserSettingsInput
{
    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_value()
    {
        var result = new UserSettingsInput("user@example.test", "user", "api-key").Validate();

        var settings = result.ShouldBeOfType<Valid<UserSettings>>().Value;
        settings.EmailAddress.ShouldBe("user@example.test");
        settings.Username.ShouldBe("user");
        settings.ApiKey.ShouldBe("api-key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_email_address_is_blank_then_it_is_valid_and_stored_empty(string enteredText)
    {
        var result = new UserSettingsInput(enteredText, "user", "api-key").Validate();

        result.ShouldBeOfType<Valid<UserSettings>>().Value.EmailAddress.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_the_email_address_has_surrounding_whitespace_then_it_is_trimmed()
    {
        var result = new UserSettingsInput("  user@example.test  ", "user", "api-key").Validate();

        result.ShouldBeOfType<Valid<UserSettings>>().Value.EmailAddress.ShouldBe("user@example.test");
    }

    [Theory]
    [InlineData("not an email")]
    [InlineData("user@")]
    [InlineData("@example.test")]
    [InlineData("User <user@example.test>")]
    [InlineData("user@example.test, other@example.test")]
    public void when_the_email_address_is_not_well_formed_then_it_is_rejected(string enteredText)
    {
        var result = new UserSettingsInput(enteredText, "user", "api-key").Validate();

        result.ShouldBeOfType<Invalid<UserSettings>>().Errors.ShouldContain(error => error.Property == nameof(UserSettingsInput.EmailAddress));
    }

    [Fact]
    public void when_created_from_an_entity_then_the_values_are_copied()
    {
        var input = UserSettingsInput.From(CreateEntity());

        input.ShouldBe(new UserSettingsInput("user@example.test", "user", "api-key"));
    }

    internal static ScrapeConfigurationEntity CreateEntity()
    {
        var id = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(id)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), id, "user@example.test", "user", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), id, "term", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), id, "root", "famous", "sub")
        };
    }
}
