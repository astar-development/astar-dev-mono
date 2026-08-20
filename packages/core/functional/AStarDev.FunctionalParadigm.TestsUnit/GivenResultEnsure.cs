namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenResultEnsure
{
    [Fact]
    public void when_ensure_is_called_on_a_success_result_then_the_finally_action_runs_and_the_same_result_is_returned()
    {
        var result = Result.Success<int, string>(4);
        bool finallyActionInvoked = false;

        var actual = result.Ensure(() => finallyActionInvoked = true);

        actual.ShouldBe(new Ok<int, string>(4));
        finallyActionInvoked.ShouldBeTrue();
    }

    [Fact]
    public void when_ensure_is_called_on_a_failure_result_then_the_finally_action_runs_and_the_same_result_is_returned()
    {
        var result = Result.Failure<int, string>("failed");
        bool finallyActionInvoked = false;

        var actual = result.Ensure(() => finallyActionInvoked = true);

        actual.ShouldBe(new Fail<int, string>("failed"));
        finallyActionInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_ensure_async_is_called_on_a_task_result_with_a_value_task_finalizer_and_the_result_is_success_then_the_finalizer_runs()
    {
        var resultTask = Task.FromResult(Result.Success<int, string>(5));
        bool finalizerInvoked = false;

        var actual = await resultTask.EnsureAsync(() =>
        {
            finalizerInvoked = true;

            return ValueTask.CompletedTask;
        });

        actual.ShouldBe(new Ok<int, string>(5));
        finalizerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_ensure_async_is_called_on_a_task_result_with_a_value_task_finalizer_and_the_result_is_failure_then_the_finalizer_runs()
    {
        var resultTask = Task.FromResult(Result.Failure<int, string>("error"));
        bool finalizerInvoked = false;

        var actual = await resultTask.EnsureAsync(() =>
        {
            finalizerInvoked = true;

            return ValueTask.CompletedTask;
        });

        actual.ShouldBe(new Fail<int, string>("error"));
        finalizerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_ensure_async_is_called_on_a_value_task_result_with_a_sync_finalizer_and_the_result_is_success_then_the_finalizer_runs()
    {
        var resultTask = ValueTask.FromResult(Result.Success<int, string>(6));
        bool finalizerInvoked = false;

        var actual = await resultTask.EnsureAsync(() => finalizerInvoked = true);

        actual.ShouldBe(new Ok<int, string>(6));
        finalizerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_ensure_async_is_called_on_a_value_task_result_with_a_sync_finalizer_and_the_result_is_failure_then_the_finalizer_runs()
    {
        var resultTask = ValueTask.FromResult(Result.Failure<int, string>("error"));
        bool finalizerInvoked = false;

        var actual = await resultTask.EnsureAsync(() => finalizerInvoked = true);

        actual.ShouldBe(new Fail<int, string>("error"));
        finalizerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_ensure_async_is_called_on_a_value_task_result_with_a_value_task_finalizer_and_the_result_is_success_then_the_finalizer_runs()
    {
        var resultTask = ValueTask.FromResult(Result.Success<int, string>(7));
        bool finalizerInvoked = false;

        var actual = await resultTask.EnsureAsync(() =>
        {
            finalizerInvoked = true;

            return ValueTask.CompletedTask;
        });

        actual.ShouldBe(new Ok<int, string>(7));
        finalizerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_ensure_async_is_called_on_a_value_task_result_with_a_value_task_finalizer_and_the_result_is_failure_then_the_finalizer_runs()
    {
        var resultTask = ValueTask.FromResult(Result.Failure<int, string>("error"));
        bool finalizerInvoked = false;

        var actual = await resultTask.EnsureAsync(() =>
        {
            finalizerInvoked = true;

            return ValueTask.CompletedTask;
        });

        actual.ShouldBe(new Fail<int, string>("error"));
        finalizerInvoked.ShouldBeTrue();
    }
}
