using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenASearchCategoriesInput
{
    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_category_in_order_with_trimmed_text()
    {
        var input = new SearchCategoriesInput([
            NewRow(" 100 ", " Nature ", includeInSearch: true, isFamous: false, isInternet: true),
            NewRow("200", "People", includeInSearch: false, isFamous: true, isInternet: false)
        ]);

        var settings = input.Validate().ShouldBeOfType<Valid<SearchCategoriesSettings>>().Value;

        settings.Categories.ShouldBe([
            new SearchCategorySettings("100", "Nature", true, false, true),
            new SearchCategorySettings("200", "People", false, true, false)
        ]);
    }

    [Fact]
    public void when_there_are_no_categories_then_the_input_is_valid() =>
        new SearchCategoriesInput([]).Validate().ShouldBeOfType<Valid<SearchCategoriesSettings>>().Value.Categories.ShouldBeEmpty();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_an_id_is_blank_then_it_is_rejected(string enteredText) =>
        ErrorsFor(new SearchCategoriesInput([NewRow(enteredText, "Nature")])).ShouldContain(error => error.Property == "SearchCategories[0].Id");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_a_name_is_blank_then_it_is_rejected(string enteredText) =>
        ErrorsFor(new SearchCategoriesInput([NewRow("100", enteredText)])).ShouldContain(error => error.Property == "SearchCategories[0].Name");

    [Theory]
    [InlineData("100", "100")]
    [InlineData("abc", "ABC")]
    [InlineData("100", " 100 ")]
    public void when_two_ids_are_the_same_ignoring_case_and_whitespace_then_the_later_one_is_rejected(string first, string second)
    {
        var errors = ErrorsFor(new SearchCategoriesInput([NewRow(first, "One"), NewRow(second, "Two")]));

        errors.Single().Property.ShouldBe("SearchCategories[1].Id");
    }

    [Fact]
    public void when_two_categories_share_a_name_then_the_input_is_valid() =>
        new SearchCategoriesInput([NewRow("100", "Same"), NewRow("200", "Same")]).Validate().ShouldBeOfType<Valid<SearchCategoriesSettings>>();

    [Fact]
    public void when_several_rows_are_invalid_then_every_error_is_reported() =>
        ErrorsFor(new SearchCategoriesInput([NewRow("", ""), NewRow("100", "One"), NewRow("100", "Two")])).Count.ShouldBe(3);

    [Fact]
    public void when_created_from_an_entity_then_existing_categories_carry_their_progress()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "100", Name = "Nature", LastKnownImageCount = 7, LastPageVisited = 3, TotalPages = 9, IncludeInSearch = false, IsFamous = true, IsInternet = true });

        var row = SearchCategoriesInput.From(entity).Categories.Single();

        row.ShouldBe(new SearchCategoryInput("100", "Nature", false, true, true, Option.Some(new SearchCategoryProgress(7, 3, 9))));
        row.IsExisting.ShouldBeTrue();
    }

    [Fact]
    public void when_an_existing_category_has_zero_progress_then_it_is_still_existing()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "100", Name = "Nature" });

        SearchCategoriesInput.From(entity).Categories.Single().IsExisting.ShouldBeTrue();
    }

    [Fact]
    public void when_a_row_has_no_progress_then_it_is_new() =>
        NewRow("100", "Nature").IsExisting.ShouldBeFalse();

    private static SearchCategoryInput NewRow(string id, string name, bool includeInSearch = true, bool isFamous = false, bool isInternet = false) =>
        new(id, name, includeInSearch, isFamous, isInternet, Option.None<SearchCategoryProgress>());

    private static IReadOnlyList<ValidationError> ErrorsFor(SearchCategoriesInput input) =>
        input.Validate().ShouldBeOfType<Invalid<SearchCategoriesSettings>>().Errors;
}
