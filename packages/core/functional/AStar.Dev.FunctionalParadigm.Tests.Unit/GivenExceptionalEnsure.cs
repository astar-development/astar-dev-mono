namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalEnsure
{
    [Fact]
    public async Task when_task_resolves_to_success_then_finalizer_receives_value_and_returns_same_result()
    {
        var resultTask = Task.FromResult<Exceptional<int>>(new Success<int>(7));
        int? finalizerValue = null;

        var actual = await resultTask.Ensure(value => finalizerValue = value);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(7));
        finalizerValue.ShouldBe(7);
    }

    [Fact]
    public async Task when_task_resolves_to_failure_then_finalizer_is_still_invoked_and_returns_same_result()
    {
        var exception = new InvalidOperationException("boom");
        var resultTask = Task.FromResult<Exceptional<int>>(new Failure<int>(exception));
        bool finalizerInvoked = false;

        var actual = await resultTask.Ensure(_ => finalizerInvoked = true);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        finalizerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_task_resolves_to_failure_then_finalizer_receives_default_value()
    {
        var resultTask = Task.FromResult<Exceptional<string>>(new Failure<string>(new InvalidOperationException("boom")));
        string finalizerValue = "not-null";

        await resultTask.Ensure(value => finalizerValue = value);

        finalizerValue.ShouldBeNull();
    }

    [Fact]
    public async Task when_chained_directly_onto_run_async_then_finalizer_runs_before_awaited_result_is_used()
    {
        bool finalizerInvoked = false;

        var actual = await Try.RunAsync(() => Task.FromResult(3))
            .Ensure(_ => finalizerInvoked = true);

        actual.ShouldBe(new Success<int>(3));
        finalizerInvoked.ShouldBeTrue();
    }
}
