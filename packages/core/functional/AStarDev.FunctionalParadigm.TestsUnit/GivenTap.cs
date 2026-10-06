namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenTap
{
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
}
