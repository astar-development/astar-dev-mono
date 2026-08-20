namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenResultBind
{
    [Fact]
    public async Task when_the_source_is_a_failure_result_and_the_binder_returns_a_value_task_then_the_binder_is_not_invoked_and_the_same_failure_is_returned()
    {
        var result = Result.Failure<int, string>("original error");
        bool binderInvoked = false;

        var actual = await result.BindAsync(value =>
        {
            binderInvoked = true;

            return ValueTask.FromResult(Result.Success<int, string>(value));
        });

        actual.ShouldBe(new Fail<int, string>("original error"));
        binderInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_source_is_a_value_task_of_a_failure_result_and_the_binder_returns_a_value_task_then_returns_the_original_failure()
    {
        var resultTask = ValueTask.FromResult(Result.Failure<int, string>("original error"));

        var actual = await resultTask.BindAsync(value => ValueTask.FromResult(Result.Success<int, string>(value)));

        actual.ShouldBe(new Fail<int, string>("original error"));
    }
}
