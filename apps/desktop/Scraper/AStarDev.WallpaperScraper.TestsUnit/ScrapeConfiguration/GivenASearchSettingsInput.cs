using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenASearchSettingsInput
{
    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_value()
    {
        var result = new SearchSettingsInput("cats", Option.Some(25)).Validate();

        var settings = result.ShouldBeOfType<Valid<SearchSettings>>().Value;
        settings.SearchTerm.ShouldBe("cats");
        settings.MaxResults.ShouldBe(Option.Some(25));
    }

    [Fact]
    public void when_the_search_term_has_surrounding_whitespace_then_it_is_trimmed() =>
        new SearchSettingsInput("  cats  ", Option.None<int>()).Validate().ShouldBeOfType<Valid<SearchSettings>>().Value.SearchTerm.ShouldBe("cats");

    [Fact]
    public void when_the_search_term_is_blank_then_the_input_is_valid_and_stored_empty() =>
        new SearchSettingsInput("   ", Option.None<int>()).Validate().ShouldBeOfType<Valid<SearchSettings>>().Value.SearchTerm.ShouldBe(string.Empty);

    [Fact]
    public void when_max_results_is_absent_then_the_input_is_valid() =>
        new SearchSettingsInput("cats", Option.None<int>()).Validate().ShouldBeOfType<Valid<SearchSettings>>().Value.MaxResults.ShouldBe(Option.None<int>());

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void when_max_results_is_not_greater_than_zero_then_it_is_rejected(int maxResults) =>
        new SearchSettingsInput("cats", Option.Some(maxResults)).Validate().ShouldBeOfType<Invalid<SearchSettings>>().Errors.ShouldContain(error => error.Property == nameof(SearchSettingsInput.MaxResults));

    [Fact]
    public void when_max_results_is_one_then_the_input_is_valid() =>
        new SearchSettingsInput("cats", Option.Some(1)).Validate().ShouldBeOfType<Valid<SearchSettings>>();

    [Fact]
    public void when_created_from_an_entity_then_the_values_are_copied() =>
        SearchSettingsInput.From(GivenAUserSettingsInput.CreateEntity()).ShouldBe(new SearchSettingsInput("term", Option.Some(10)));

    [Fact]
    public void when_created_from_an_entity_without_max_results_then_it_is_none()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        entity.SearchConfiguration.MaxResults = null;

        SearchSettingsInput.From(entity).MaxResults.ShouldBe(Option.None<int>());
    }

    [Fact]
    public void when_created_from_an_entity_with_zero_max_results_then_it_is_some_zero()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        entity.SearchConfiguration.MaxResults = 0;

        SearchSettingsInput.From(entity).MaxResults.ShouldBe(Option.Some(0));
    }
}
