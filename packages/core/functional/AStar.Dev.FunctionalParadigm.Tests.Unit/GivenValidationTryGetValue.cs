namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public sealed class GivenValidationTryGetValue
{
    [Fact]
    public void when_try_get_value_is_called_on_valid_then_returns_true_and_outputs_the_value()
    {
        var validation = Validation.Valid(42);

        bool result = validation.TryGetValue(out int value);

        result.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void when_try_get_value_is_called_on_invalid_then_returns_false_and_outputs_the_default()
    {
        var validation = Validation.Invalid<int>(ValidationErrorFactory.Create("Name", "required"));

        bool result = validation.TryGetValue(out int value);

        result.ShouldBeFalse();
        value.ShouldBe(0);
    }

    [Fact]
    public void when_try_get_errors_is_called_on_invalid_then_returns_true_and_outputs_the_errors()
    {
        var errors = new List<ValidationError>
        {
            ValidationErrorFactory.Create("Name", "required"),
            ValidationErrorFactory.Create("Age", "must be positive")
        };
        var validation = Validation.Invalid<int>(errors);

        bool result = validation.TryGetErrors(out var outErrors);

        result.ShouldBeTrue();
        outErrors.ShouldBe(errors);
    }

    [Fact]
    public void when_try_get_errors_is_called_on_valid_then_returns_false_and_outputs_an_empty_list()
    {
        var validation = Validation.Valid(42);

        bool result = validation.TryGetErrors(out var outErrors);

        result.ShouldBeFalse();
        outErrors.ShouldBeEmpty();
    }
}
