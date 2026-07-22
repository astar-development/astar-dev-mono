namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalToResult
{
    [Fact]
    public void when_exceptional_is_success_then_maps_to_ok_result()
    {
        var exceptional = Exceptional.Success(5);

        var actual = exceptional.ToResult(ex => ex.Message);

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(5));
    }

    [Fact]
    public void when_exceptional_is_failure_then_maps_error_via_selector()
    {
        var exception = new InvalidOperationException("bad thing");
        var exceptional = Exceptional.Failure<int>(exception);

        var actual = exceptional.ToResult(ex => ex.Message);

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("bad thing"));
    }
}
