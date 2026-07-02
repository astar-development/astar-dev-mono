using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenMap
{
    [Fact]
    public void when_selector_is_value_then_returns_expected_result()
    {
        var result = Result.Success<int, string>(5);

        var actual = result.Map(value => value * 3);

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(15));
    }

    [Fact]
    public void when_result_is_failure_then_returns_failure()
    {
        var result = Result.Failure<int, string>("error");

        var actual = result.Map(value => value * 3);

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("error"));
    }
}
