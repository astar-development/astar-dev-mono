using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAPersonCategoriesInput
{
    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_category_in_order_with_trimmed_names()
    {
        var existingId = Guid.CreateVersion7();
        var input = new PersonCategoriesInput([
            new PersonCategoryInput(Option.Some(existingId), " Celebrities "),
            new PersonCategoryInput(Option.None<Guid>(), "Models")
        ]);

        var settings = input.Validate().ShouldBeOfType<Valid<PersonCategoriesSettings>>().Value;

        settings.Categories.ShouldBe([
            new PersonCategorySettings(Option.Some(existingId), "Celebrities"),
            new PersonCategorySettings(Option.None<Guid>(), "Models")
        ]);
    }

    [Fact]
    public void when_there_are_no_categories_then_the_input_is_valid() =>
        new PersonCategoriesInput([]).Validate().ShouldBeOfType<Valid<PersonCategoriesSettings>>().Value.Categories.ShouldBeEmpty();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_a_name_is_blank_then_it_is_rejected(string enteredText) =>
        ErrorsFor(new PersonCategoriesInput([New(enteredText)])).ShouldContain(error => error.Property == "PersonCategories[0].Name");

    [Theory]
    [InlineData("Models", "Models")]
    [InlineData("Models", "MODELS")]
    [InlineData("Models", " models ")]
    public void when_two_names_are_the_same_ignoring_case_and_whitespace_then_the_later_one_is_rejected(string first, string second) =>
        ErrorsFor(new PersonCategoriesInput([New(first), New(second)])).Single().Property.ShouldBe("PersonCategories[1].Name");

    [Fact]
    public void when_several_rows_are_invalid_then_every_error_is_reported() =>
        ErrorsFor(new PersonCategoriesInput([New(""), New("Models"), New("models")])).Count.ShouldBe(2);

    [Fact]
    public void when_created_from_an_entity_then_each_category_is_an_existing_row()
    {
        var entity = GivenAUserSettingsInput.CreateEntity();
        var category = new PersonCategoryEntity { Name = "Celebrities" };
        entity.SearchConfiguration.PersonCategories.Add(category);

        var row = PersonCategoriesInput.From(entity).Categories.Single();

        row.ShouldBe(new PersonCategoryInput(Option.Some(category.Id), "Celebrities"));
    }

    [Fact]
    public void when_the_entity_has_no_person_categories_then_the_input_is_empty() =>
        PersonCategoriesInput.From(GivenAUserSettingsInput.CreateEntity()).Categories.ShouldBeEmpty();

    private static PersonCategoryInput New(string name) => new(Option.None<Guid>(), name);

    private static IReadOnlyList<ValidationError> ErrorsFor(PersonCategoriesInput input) =>
        input.Validate().ShouldBeOfType<Invalid<PersonCategoriesSettings>>().Errors;
}
