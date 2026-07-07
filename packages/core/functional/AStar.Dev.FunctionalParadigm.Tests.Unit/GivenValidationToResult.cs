using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenValidationToResult
{
    [Fact]
    public void when_validation_is_valid_then_maps_to_ok_result()
    {
        var validation = Validation.Valid(9);

        var actual = validation.ToResult(errors => string.Join(",", errors.Select(e => e.Message)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(9));
    }

    [Fact]
    public void when_validation_is_invalid_then_maps_errors_via_selector()
    {
        var errors = new List<ValidationError> { ValidationErrorFactory.Create("Name", "required") };
        var validation = Validation.Invalid<int>(errors);

        var actual = validation.ToResult(e => string.Join(",", e.Select(x => x.Message)));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("required"));
    }
}
