using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenPersonCategoriesSettings
{
    [Fact]
    public void when_an_existing_category_is_renamed_then_the_same_row_is_updated()
    {
        var entity = CreateEntity("Celebrities");
        var original = entity.SearchConfiguration.PersonCategories.Single();
        var id = original.Id;
        original.UpdatedAt = DateTimeOffset.UnixEpoch;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

        new PersonCategoriesSettings([new PersonCategorySettings(Option.Some(id), "Famous People")]).ApplyTo(entity, now);

        var renamed = entity.SearchConfiguration.PersonCategories.Single();
        renamed.ShouldBeSameAs(original);
        renamed.Id.ShouldBe(id);
        renamed.Name.ShouldBe("Famous People");
        renamed.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void when_a_category_is_added_then_it_is_created_for_the_search_configuration()
    {
        var entity = CreateEntity("Celebrities");
        var existingId = entity.SearchConfiguration.PersonCategories.Single().Id;

        new PersonCategoriesSettings([
            new PersonCategorySettings(Option.Some(existingId), "Celebrities"),
            new PersonCategorySettings(Option.None<Guid>(), "Models")
        ]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        var added = entity.SearchConfiguration.PersonCategories.Single(category => category.Name == "Models");
        added.SearchConfigurationId.ShouldBe(entity.SearchConfiguration.Id);
        added.Id.ShouldNotBe(existingId);
        entity.SearchConfiguration.PersonCategories.Count.ShouldBe(2);
    }

    [Fact]
    public void when_a_category_is_missing_from_the_settings_then_it_is_removed()
    {
        var entity = CreateEntity("Celebrities", "Models");
        var keepId = entity.SearchConfiguration.PersonCategories.Single(category => category.Name == "Models").Id;

        new PersonCategoriesSettings([new PersonCategorySettings(Option.Some(keepId), "Models")]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.PersonCategories.Select(category => category.Name).ShouldBe(["Models"]);
    }

    [Fact]
    public void when_the_settings_are_empty_then_every_category_is_removed()
    {
        var entity = CreateEntity("Celebrities", "Models");

        new PersonCategoriesSettings([]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.PersonCategories.ShouldBeEmpty();
    }

    [Fact]
    public void when_an_id_is_not_found_then_the_category_is_added_as_new()
    {
        var entity = CreateEntity("Celebrities");

        new PersonCategoriesSettings([new PersonCategorySettings(Option.Some(Guid.CreateVersion7()), "Unknown")]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.PersonCategories.Select(category => category.Name).ShouldBe(["Unknown"]);
    }

    [Fact]
    public void when_applied_then_the_search_categories_and_other_sections_are_untouched()
    {
        var entity = CreateEntity("Celebrities");
        entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "100", Name = "Nature" });

        new PersonCategoriesSettings([]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("Nature");
        entity.SearchConfiguration.SearchTerm.ShouldBe("term");
        entity.UserConfiguration.Username.ShouldBe("user");
    }

    private static ScrapeConfigurationEntity CreateEntity(params string[] names)
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        foreach (var name in names) entity.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = entity.SearchConfiguration.Id, Name = name });

        return entity;
    }
}
