using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenSearchSettings
{
    [Fact]
    public void when_applied_to_an_entity_then_the_search_values_are_replaced_and_the_identity_categories_and_other_sections_are_untouched()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        var searchId = entity.SearchConfiguration.Id;

        new SearchSettings("dogs", Option.Some(50)).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.SearchTerm.ShouldBe("dogs");
        entity.SearchConfiguration.MaxResults.ShouldBe(50);
        entity.SearchConfiguration.Id.ShouldBe(searchId);
        entity.UserConfiguration.Username.ShouldBe("user");
        entity.ScrapeDirectories.RootDirectory.ShouldBe("root");
    }

    [Fact]
    public void when_applied_to_an_entity_then_the_search_configuration_modified_time_is_refreshed()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        entity.SearchConfiguration.UpdatedAt = DateTimeOffset.UnixEpoch;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

        new SearchSettings("dogs", Option.Some(50)).ApplyTo(entity, now);

        entity.SearchConfiguration.UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public void when_applied_with_no_max_results_then_the_entity_max_results_is_null()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();

        new SearchSettings("dogs", Option.None<int>()).ApplyTo(entity, DateTimeOffset.UnixEpoch);

        entity.SearchConfiguration.MaxResults.ShouldBeNull();
    }

    [Fact]
    public void when_created_from_an_entity_then_the_values_are_copied() =>
        SearchSettings.From(GivenAUserSettingsInput.CreateEntity()).ShouldBe(new SearchSettings("term", Option.Some(10)));
}
