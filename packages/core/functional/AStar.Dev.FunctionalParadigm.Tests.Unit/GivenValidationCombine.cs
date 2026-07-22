namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenValidationCombine
{
    [Fact]
    public void when_all_validations_are_valid_then_returns_valid_with_ordered_values()
    {
        var validations = new[] { Validation.Valid(1), Validation.Valid(2), Validation.Valid(3) };

        var actual = validations.Combine();

        actual.ShouldBeOfType<Valid<IReadOnlyList<int>>>();
        var valid = (Valid<IReadOnlyList<int>>)actual;
        valid.Value.ShouldBe(new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void when_one_validation_is_invalid_then_returns_invalid_with_its_errors()
    {
        var error = ValidationErrorFactory.Create("Age", "must be positive");
        var validations = new[] { Validation.Valid(1), Validation.Invalid<int>(error), Validation.Valid(3) };

        var actual = validations.Combine();

        actual.ShouldBeOfType<Invalid<IReadOnlyList<int>>>();
        var invalid = (Invalid<IReadOnlyList<int>>)actual;
        invalid.Errors.Count.ShouldBe(1);
        invalid.Errors[0].ShouldBe(error);
    }

    [Fact]
    public void when_multiple_validations_are_invalid_then_accumulates_all_errors_in_order()
    {
        var firstError = ValidationErrorFactory.Create("Name", "required");
        var secondError = ValidationErrorFactory.Create("Age", "must be positive");
        var validations = new[] { Validation.Invalid<int>(firstError), Validation.Valid(2), Validation.Invalid<int>(secondError) };

        var actual = validations.Combine();

        actual.ShouldBeOfType<Invalid<IReadOnlyList<int>>>();
        var invalid = (Invalid<IReadOnlyList<int>>)actual;
        invalid.Errors.Count.ShouldBe(2);
        invalid.Errors[0].ShouldBe(firstError);
        invalid.Errors[1].ShouldBe(secondError);
    }

    [Fact]
    public void when_input_is_empty_then_returns_valid_with_empty_list()
    {
        var validations = Array.Empty<Validation<int>>();

        var actual = validations.Combine();

        actual.ShouldBeOfType<Valid<IReadOnlyList<int>>>();
        var valid = (Valid<IReadOnlyList<int>>)actual;
        valid.Value.ShouldBeEmpty();
    }
}
