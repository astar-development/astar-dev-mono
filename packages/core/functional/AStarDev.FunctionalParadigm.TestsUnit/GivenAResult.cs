namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenAResult
{
    [Fact]
    public void when_success_is_called_then_returns_an_ok_wrapping_the_value()
    {
        var result = Result.Success<int, string>(42);

        result.ShouldBeOfType<Ok<int, string>>();
        result.ShouldBe(new Ok<int, string>(42));
    }

    [Fact]
    public void when_failure_is_called_then_returns_a_fail_wrapping_the_error()
    {
        var result = Result.Failure<int, string>("boom");

        result.ShouldBeOfType<Fail<int, string>>();
        result.ShouldBe(new Fail<int, string>("boom"));
    }

    [Fact]
    public void when_two_ok_results_have_equal_values_then_they_are_equal() =>
        new Ok<int, string>(7).ShouldBe(new Ok<int, string>(7));

    [Fact]
    public void when_two_ok_results_have_different_values_then_they_are_not_equal() =>
        new Ok<int, string>(7).ShouldNotBe(new Ok<int, string>(8));

    [Fact]
    public void when_two_fail_results_have_equal_errors_then_they_are_equal() =>
        new Fail<int, string>("error").ShouldBe(new Fail<int, string>("error"));

    [Fact]
    public void when_two_fail_results_have_different_errors_then_they_are_not_equal() =>
        new Fail<int, string>("error").ShouldNotBe(new Fail<int, string>("other"));

    [Fact]
    public void when_an_ok_result_is_compared_to_a_fail_result_then_they_are_not_equal()
    {
        Result<int, string> ok = new Ok<int, string>(7);
        Result<int, string> fail = new Fail<int, string>("error");

        ok.ShouldNotBe(fail);
    }
}
