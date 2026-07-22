namespace AStar.Dev.Utilities.Tests.Unit;

public class GivenLinqExtensions
{
    [Fact]
    public void when_for_each_is_called_on_an_ienumerable_then_the_supplied_action_is_triggered()
    {
        var exception = Record.Exception(() => new List<string> { "", "z", "a" }.AsEnumerable().ForEach(_ => { }));

        Assert.Null(exception);
    }

    [Fact]
    public void when_for_each_is_called_with_a_null_enumerable_then_does_nothing()
    {
        IEnumerable<string>? enumerable = null;

        var exception = Record.Exception(() => enumerable!.ForEach(_ => { }));

        Assert.Null(exception);
    }

    [Fact]
    public void when_for_each_is_called_with_a_null_action_then_does_nothing()
    {
        Action<string>? action = null;

        var exception = Record.Exception(() => new List<string> { "a" }.AsEnumerable().ForEach(action!));

        Assert.Null(exception);
    }

    [Fact]
    public void when_for_each_is_called_then_the_supplied_action_runs_for_every_item()
    {
        List<string> visited = [];

        new List<string> { "a", "b", "c" }.AsEnumerable().ForEach(item => visited.Add(item));

        visited.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public async Task when_for_each_async_is_called_on_an_ienumerable_then_the_supplied_action_runs_for_every_item_in_order()
    {
        List<string> visited = [];

        await new List<string> { "a", "b", "c" }.ForEachAsync(item =>
        {
            visited.Add(item);

            return Task.CompletedTask;
        });

        visited.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public async Task when_for_each_async_is_called_then_each_item_is_awaited_before_the_next_action_starts()
    {
        List<string> completionOrder = [];

        await new List<int> { 1, 2, 3 }.ForEachAsync(async item =>
        {
            await Task.Delay(3 - item);
            completionOrder.Add($"start-{item}");
            await Task.Delay(1);
            completionOrder.Add($"end-{item}");
        });

        completionOrder.ShouldBe(["start-1", "end-1", "start-2", "end-2", "start-3", "end-3"]);
    }
}
