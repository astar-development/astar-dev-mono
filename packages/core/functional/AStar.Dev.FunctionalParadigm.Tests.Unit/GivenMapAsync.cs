using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenMapAsync
{
    [Fact]
    public async Task when_selector_is_task_then_returns_expected_result()
    {
        var result = Result.Success<int, string>(5);

        var actual = await result.MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(10));
    }

    [Fact]
    public async Task when_selector_is_value_task_then_returns_expected_result()
    {
        var result = Result.Success<int, string>(5);

        var actual = await result.MapAsync(value => ValueTask.FromResult(value * 2));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(10));
    }

    [Fact]
    public async Task when_result_is_failure_then_returns_failure()
    {
        var result = Result.Failure<int, string>("error");

        var actual = await result.MapAsync(value => ValueTask.FromResult(value * 2));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("error"));
    }
}
