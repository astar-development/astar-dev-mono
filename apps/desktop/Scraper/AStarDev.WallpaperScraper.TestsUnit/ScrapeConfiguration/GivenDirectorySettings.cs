using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenDirectorySettings
{
    [Fact]
    public void when_applied_to_an_entity_then_the_directories_are_replaced_and_the_identity_and_other_sections_are_untouched()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        var directoriesId = entity.ScrapeDirectories.Id;

        new DirectorySettings("/new/root", "/new/famous", "new-sub").ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.ScrapeDirectories.RootDirectory.ShouldBe("/new/root");
        entity.ScrapeDirectories.RootDirectoryFamous.ShouldBe("/new/famous");
        entity.ScrapeDirectories.SubDirectoryName.ShouldBe("new-sub");
        entity.ScrapeDirectories.Id.ShouldBe(directoriesId);
        entity.ScrapeDirectories.ScrapeConfigurationEntityId.ShouldBe(entity.Id);
        entity.UserConfiguration.Username.ShouldBe("user");
        entity.SearchConfiguration.SearchTerm.ShouldBe("term");
    }

    [Fact]
    public void when_created_from_an_entity_then_the_values_are_copied() =>
        DirectorySettings.From(GivenAUserSettingsInput.CreateEntity()).ShouldBe(new DirectorySettings("root", "famous", "sub"));
}
