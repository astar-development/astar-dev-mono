using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenSearchCategoriesSettings
{
    [Fact]
    public void when_an_existing_category_is_edited_then_its_editable_values_change_and_its_progress_is_preserved()
    {
        var entity = CreateEntity();
        var original = entity.SearchConfiguration.SearchCategories.Single();
        original.UpdatedAt = DateTimeOffset.UnixEpoch;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

        new SearchCategoriesSettings([new SearchCategorySettings("100", "Renamed", false, true, true)]).ApplyTo(entity, now);

        var edited = entity.SearchConfiguration.SearchCategories.Single();
        edited.ShouldBeSameAs(original);
        edited.Name.ShouldBe("Renamed");
        edited.IncludeInSearch.ShouldBeFalse();
        edited.IsFamous.ShouldBeTrue();
        edited.IsInternet.ShouldBeTrue();
        (edited.LastKnownImageCount, edited.LastPageVisited, edited.TotalPages).ShouldBe((7, 3, 9));
        edited.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void when_the_id_differs_only_by_case_then_the_existing_category_is_edited_and_keeps_its_id()
    {
        var entity = CreateEntity("Abc");

        new SearchCategoriesSettings([new SearchCategorySettings("aBC", "Renamed", true, false, false)]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        var edited = entity.SearchConfiguration.SearchCategories.Single();
        edited.Id.ShouldBe("Abc");
        edited.Name.ShouldBe("Renamed");
    }

    [Fact]
    public void when_a_category_is_added_then_it_is_created_for_the_search_configuration_with_no_progress()
    {
        var entity = CreateEntity();

        new SearchCategoriesSettings([
            new SearchCategorySettings("100", "Nature", true, false, false),
            new SearchCategorySettings("200", "People", false, true, true)
        ]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        var added = entity.SearchConfiguration.SearchCategories.Single(category => category.Id == "200");
        added.Name.ShouldBe("People");
        added.IncludeInSearch.ShouldBeFalse();
        added.IsFamous.ShouldBeTrue();
        added.IsInternet.ShouldBeTrue();
        added.SearchConfigurationId.ShouldBe(entity.SearchConfiguration.Id);
        (added.LastKnownImageCount, added.LastPageVisited, added.TotalPages).ShouldBe((0, 0, 0));
        entity.SearchConfiguration.SearchCategories.Count.ShouldBe(2);
    }

    [Fact]
    public void when_a_category_is_missing_from_the_settings_then_it_is_removed()
    {
        var entity = CreateEntity();
        entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "200", Name = "People" });

        new SearchCategoriesSettings([new SearchCategorySettings("200", "People", true, false, false)]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.SearchCategories.Select(category => category.Id).ShouldBe(["200"]);
    }

    [Fact]
    public void when_the_settings_are_empty_then_every_category_is_removed()
    {
        var entity = CreateEntity();

        new SearchCategoriesSettings([]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.SearchCategories.ShouldBeEmpty();
    }

    [Fact]
    public void when_applied_then_the_other_sections_and_the_person_categories_are_untouched()
    {
        var entity = CreateEntity();
        entity.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { Name = "Models" });

        new SearchCategoriesSettings([]).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.SearchTerm.ShouldBe("term");
        entity.SearchConfiguration.PersonCategories.Single().Name.ShouldBe("Models");
        entity.UserConfiguration.Username.ShouldBe("user");
    }

    private static ScrapeConfigurationEntity CreateEntity(string categoryId = "100")
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity
        {
            SearchConfigurationId = entity.SearchConfiguration.Id,
            Id = categoryId,
            Name = "Nature",
            LastKnownImageCount = 7,
            LastPageVisited = 3,
            TotalPages = 9
        });

        return entity;
    }
}
