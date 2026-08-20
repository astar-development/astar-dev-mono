namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenResultMap
{
    [Fact]
    public async Task when_map_async_with_a_task_selector_is_called_on_a_failure_then_the_selector_is_not_invoked_and_the_same_failure_is_returned()
    {
        var result = Result.Failure<int, string>("error");
        bool selectorInvoked = false;

        var actual = await result.MapAsync(value =>
        {
            selectorInvoked = true;

            return Task.FromResult(value * 2);
        });

        actual.ShouldBe(new Fail<int, string>("error"));
        selectorInvoked.ShouldBeFalse();
    }
}
