namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenMatchAsync
{
    [Fact]
    public async Task when_on_success_is_async_then_returns_expected_value()
    {
        var result = Result.Success<int, string>(3);

        int actual = await result.MatchAsync(
            onSuccess: value => ValueTask.FromResult(value + 1),
            onFailure: _ => -1);

        actual.ShouldBe(4);
    }

    [Fact]
    public async Task when_on_failure_is_async_then_returns_expected_value()
    {
        var result = Result.Failure<int, string>("error");

        int actual = await result.MatchAsync(
            onSuccess: _ => 1,
            onFailure: error => ValueTask.FromResult(error.Length));

        actual.ShouldBe(5);
    }

    [Fact]
    public async Task when_on_success_and_on_failure_are_both_async_then_returns_expected_values()
    {
        var success = Result.Success<int, string>(4);
        var failure = Result.Failure<int, string>("error");

        int actualSuccess = await success.MatchAsync(
            onSuccess: value => ValueTask.FromResult(value + 2),
            onFailure: error => ValueTask.FromResult(-1));

        int actualFailure = await failure.MatchAsync(
            onSuccess: value => ValueTask.FromResult(value + 2),
            onFailure: error => ValueTask.FromResult(error.Length));

        actualSuccess.ShouldBe(6);
        actualFailure.ShouldBe(5);
    }
}
