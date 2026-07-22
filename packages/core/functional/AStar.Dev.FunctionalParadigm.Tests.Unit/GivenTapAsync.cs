namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenTapAsync
{
    [Fact]
    public async Task when_called_on_task_result_then_returns_same_result_and_executes_side_effect()
    {
        var resultTask = Task.FromResult(Result.Success<int, string>(7));
        bool sideEffect = false;

        var actual = await resultTask.TapAsync(value => sideEffect = value == 7);

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(7));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_called_on_value_task_result_then_returns_same_result_and_executes_side_effect()
    {
        var resultTask = ValueTask.FromResult(Result.Success<int, string>(9));
        bool sideEffect = false;

        var actual = await resultTask.TapAsync(value => sideEffect = value == 9);

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(9));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_called_on_task_failure_then_returns_same_failure_and_executes_failure_handler()
    {
        var resultTask = Task.FromResult(Result.Failure<int, string>("error"));
        bool sideEffect = false;

        var actual = await resultTask.TapAsync(_ => { }, error => sideEffect = error == "error");

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("error"));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_called_on_task_result_then_returns_same_result_and_executes_finalizer()
    {
        var resultTask = Task.FromResult(Result.Success<int, string>(7));
        bool finalizerCalled = false;

        var actual = await resultTask.EnsureAsync(() => finalizerCalled = true);

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(7));
        finalizerCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task when_called_on_task_failure_then_returns_same_failure_and_executes_finalizer()
    {
        var resultTask = Task.FromResult(Result.Failure<int, string>("error"));
        bool finalizerCalled = false;

        var actual = await resultTask.EnsureAsync(() => finalizerCalled = true);

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("error"));
        finalizerCalled.ShouldBeTrue();
    }
}
