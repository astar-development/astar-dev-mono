using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenValidationApply
{
    [Fact]
    public void when_both_are_valid_then_applies_function_to_value()
    {
        var validationFunc = Validation.Valid<Func<int, int>>(value => value * 2);
        var validationValue = Validation.Valid(5);

        var actual = validationFunc.Apply(validationValue);

        actual.ShouldBeOfType<Valid<int>>();
        actual.ShouldBe(new Valid<int>(10));
    }

    [Fact]
    public void when_function_is_invalid_and_value_is_valid_then_returns_function_errors()
    {
        var funcError = ValidationErrorFactory.Create("Func", "broken");
        var validationFunc = Validation.Invalid<Func<int, int>>(funcError);
        var validationValue = Validation.Valid(5);

        var actual = validationFunc.Apply(validationValue);

        actual.ShouldBeOfType<Invalid<int>>();
        var invalid = (Invalid<int>)actual;
        invalid.Errors.Count.ShouldBe(1);
        invalid.Errors[0].ShouldBe(funcError);
    }

    [Fact]
    public void when_function_is_valid_and_value_is_invalid_then_returns_value_errors()
    {
        var valueError = ValidationErrorFactory.Create("Value", "required");
        var validationFunc = Validation.Valid<Func<int, int>>(value => value * 2);
        var validationValue = Validation.Invalid<int>(valueError);

        var actual = validationFunc.Apply(validationValue);

        actual.ShouldBeOfType<Invalid<int>>();
        var invalid = (Invalid<int>)actual;
        invalid.Errors.Count.ShouldBe(1);
        invalid.Errors[0].ShouldBe(valueError);
    }

    [Fact]
    public void when_both_are_invalid_then_accumulates_both_error_sets_in_order()
    {
        var funcError = ValidationErrorFactory.Create("Func", "broken");
        var valueError = ValidationErrorFactory.Create("Value", "required");
        var validationFunc = Validation.Invalid<Func<int, int>>(funcError);
        var validationValue = Validation.Invalid<int>(valueError);

        var actual = validationFunc.Apply(validationValue);

        actual.ShouldBeOfType<Invalid<int>>();
        var invalid = (Invalid<int>)actual;
        invalid.Errors.Count.ShouldBe(2);
        invalid.Errors[0].ShouldBe(funcError);
        invalid.Errors[1].ShouldBe(valueError);
    }
}
