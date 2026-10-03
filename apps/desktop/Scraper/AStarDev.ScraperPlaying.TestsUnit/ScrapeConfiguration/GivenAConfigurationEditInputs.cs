using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAConfigurationEditInputs
{
    [Fact]
    public void when_the_inputs_are_created_from_a_configuration_then_every_section_is_read_from_it()
    {
        var entity = CreateEntity();

        var inputs = ConfigurationEditInputs.From(entity);

        ShouldMatch(inputs, RootSettingsInput.From(entity), UserSettingsInput.From(entity), DirectorySettingsInput.From(entity), SearchSettingsInput.From(entity), SearchCategoriesInput.From(entity), PersonCategoriesInput.From(entity));
    }

    [Fact]
    public void when_the_configuration_is_loaded_for_editing_then_it_carries_its_id_and_inputs()
    {
        var entity = CreateEntity();

        var loaded = LoadedConfiguration.From(entity);

        loaded.Id.ShouldBe(entity.Id);
        ShouldMatchEntity(loaded.Inputs, entity);
    }

    internal static void ShouldMatchEntity(ConfigurationEditInputs inputs, ScrapeConfigurationEntity entity)
        => ShouldMatch(inputs, RootSettingsInput.From(entity), UserSettingsInput.From(entity), DirectorySettingsInput.From(entity), SearchSettingsInput.From(entity), SearchCategoriesInput.From(entity), PersonCategoriesInput.From(entity));

    private static void ShouldMatch(ConfigurationEditInputs inputs, RootSettingsInput root, UserSettingsInput user, DirectorySettingsInput directories, SearchSettingsInput search, SearchCategoriesInput searchCategories, PersonCategoriesInput personCategories)
    {
        (inputs.Root, inputs.User, inputs.Directories, inputs.Search).ShouldBe((root, user, directories, search));
        inputs.SearchCategories.Categories.ShouldBe(searchCategories.Categories);
        inputs.PersonCategories.Categories.ShouldBe(personCategories.Categories);
    }

    internal static ScrapeConfigurationEntity CreateEntity()
    {
        var id = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(id)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), id, "user@example.test", "user", "user-key"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), id, "term", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), id, "root", "famous", "sub"),
            BaseUrl = new Uri("https://wallhaven.cc/api/v1"),
            LoginUrl = "https://wallhaven.cc/login",
            ApiKey = "api-key",
            SearchString = "cats",
            TopWallpapers = "top",
            HotWallpapers = "hot",
            HotWallpapersStartingPageNumber = 7,
            HotWallpapersTotalPages = 80,
            SearchStringPrefix = "prefix-",
            SearchStringSuffix = "-suffix",
            Subscriptions = "subscriptions",
            ImagePauseInSeconds = 5,
            TotalPages = 20,
            UseHeadless = true
        };
    }
}
