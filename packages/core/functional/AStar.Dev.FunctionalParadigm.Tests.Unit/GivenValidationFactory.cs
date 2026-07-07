using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenValidationFactory
{
    [Fact]
    public void when_valid_created_then_wraps_value()
    {
        var actual = Validation.Valid(42);

        actual.ShouldBeOfType<Valid<int>>();
        actual.ShouldBe(new Valid<int>(42));
    }

    [Fact]
    public void when_invalid_created_with_error_list_then_wraps_errors()
    {
        var errors = new List<ValidationError> { ValidationErrorFactory.Create("Name", "Required") };

        var actual = Validation.Invalid<int>(errors);

        actual.ShouldBeOfType<Invalid<int>>();
        var invalid = (Invalid<int>)actual;
        invalid.Errors.Count.ShouldBe(1);
        invalid.Errors[0].ShouldBe(errors[0]);
    }

    [Fact]
    public void when_invalid_created_with_single_error_then_wraps_that_error()
    {
        var error = ValidationErrorFactory.Create("Name", "Required");

        var actual = Validation.Invalid<int>(error);

        actual.ShouldBeOfType<Invalid<int>>();
        var invalid = (Invalid<int>)actual;
        invalid.Errors.Count.ShouldBe(1);
        invalid.Errors[0].ShouldBe(error);
    }
}
