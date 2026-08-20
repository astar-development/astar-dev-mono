namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenResultMatch
{
    [Fact]
    public async Task when_match_async_is_called_on_a_result_with_task_returning_handlers_then_returns_the_expected_values()
    {
        var success = Result.Success<int, string>(4);
        var failure = Result.Failure<int, string>("error");

        int actualSuccess = await success.MatchAsync(onSuccess: value => Task.FromResult(value + 1), onFailure: error => Task.FromResult(-1));
        int actualFailure = await failure.MatchAsync(onSuccess: value => Task.FromResult(value + 1), onFailure: error => Task.FromResult(error.Length));

        actualSuccess.ShouldBe(5);
        actualFailure.ShouldBe(5);
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_task_result_with_task_returning_handlers_then_awaits_the_result_and_returns_the_expected_values()
    {
        var successTask = Task.FromResult(Result.Success<int, string>(3));
        var failureTask = Task.FromResult(Result.Failure<int, string>("error"));

        int actualSuccess = await successTask.MatchAsync(onSuccess: value => Task.FromResult(value * 2), onFailure: error => Task.FromResult(-1));
        int actualFailure = await failureTask.MatchAsync(onSuccess: value => Task.FromResult(value * 2), onFailure: error => Task.FromResult(error.Length));

        actualSuccess.ShouldBe(6);
        actualFailure.ShouldBe(5);
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_task_result_with_sync_handlers_then_awaits_the_result_and_returns_the_expected_values()
    {
        var successTask = Task.FromResult(Result.Success<int, string>(2));
        var failureTask = Task.FromResult(Result.Failure<int, string>("error"));

        int actualSuccess = await successTask.MatchAsync(onSuccess: value => value * 3, onFailure: error => -1);
        int actualFailure = await failureTask.MatchAsync(onSuccess: value => value * 3, onFailure: error => error.Length);

        actualSuccess.ShouldBe(6);
        actualFailure.ShouldBe(5);
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_result_with_a_sync_on_success_and_a_value_task_on_failure_then_returns_the_expected_values()
    {
        var success = Result.Success<int, string>(7);
        var failure = Result.Failure<int, string>("error");

        int actualSuccess = await success.MatchAsync(onSuccess: value => value + 1, onFailure: error => ValueTask.FromResult(-1));
        int actualFailure = await failure.MatchAsync(onSuccess: value => value + 1, onFailure: error => ValueTask.FromResult(error.Length));

        actualSuccess.ShouldBe(8);
        actualFailure.ShouldBe(5);
    }
}
