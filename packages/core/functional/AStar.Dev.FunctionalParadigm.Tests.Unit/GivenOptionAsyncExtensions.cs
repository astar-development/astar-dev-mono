namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenOptionAsyncExtensions
{
    [Fact]
    public async Task when_map_async_is_called_on_an_option_with_some_then_transforms_the_value()
    {
        var actual = await Option.Some(42).MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBe(Option.Some(84));
    }

    [Fact]
    public async Task when_map_async_is_called_on_an_option_with_none_then_returns_none()
    {
        var actual = await Option.None<int>().MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBe(Option.None<int>());
    }

    [Fact]
    public async Task when_map_async_is_called_on_a_task_of_option_with_a_sync_mapper_then_transforms_the_value()
    {
        var actual = await Task.FromResult(Option.Some(42)).MapAsync(value => value * 2);

        actual.ShouldBe(Option.Some(84));
    }

    [Fact]
    public async Task when_map_async_is_called_on_a_task_of_option_with_an_async_mapper_then_transforms_the_value()
    {
        var actual = await Task.FromResult(Option.Some(42)).MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBe(Option.Some(84));
    }

    [Fact]
    public async Task when_map_async_is_called_on_a_task_of_none_with_an_async_mapper_then_returns_none()
    {
        var actual = await Task.FromResult(Option.None<int>()).MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBe(Option.None<int>());
    }

    [Fact]
    public async Task when_bind_async_is_called_on_an_option_with_some_then_chains_the_bound_option()
    {
        var actual = await Option.Some(42).BindAsync(value => Task.FromResult(Option.Some(value * 2)));

        actual.ShouldBe(Option.Some(84));
    }

    [Fact]
    public async Task when_bind_async_is_called_on_an_option_with_none_then_returns_none()
    {
        var actual = await Option.None<int>().BindAsync(value => Task.FromResult(Option.Some(value * 2)));

        actual.ShouldBe(Option.None<int>());
    }

    [Fact]
    public async Task when_bind_async_is_called_on_a_task_of_option_with_a_sync_binder_then_chains_the_bound_option()
    {
        var actual = await Task.FromResult(Option.Some(42)).BindAsync(value => Option.Some(value * 2));

        actual.ShouldBe(Option.Some(84));
    }

    [Fact]
    public async Task when_bind_async_is_called_on_a_task_of_option_with_an_async_binder_then_chains_the_bound_option()
    {
        var actual = await Task.FromResult(Option.Some(42)).BindAsync(value => Task.FromResult(Option.Some(value * 2)));

        actual.ShouldBe(Option.Some(84));
    }

    [Fact]
    public async Task when_bind_async_is_called_on_a_task_of_none_with_an_async_binder_then_returns_none()
    {
        var actual = await Task.FromResult(Option.None<int>()).BindAsync(value => Task.FromResult(Option.Some(value * 2)));

        actual.ShouldBe(Option.None<int>());
    }

    [Fact]
    public async Task when_tap_async_is_called_on_an_option_with_some_then_invokes_the_action_and_returns_the_original_option()
    {
        int tappedValue = 0;

        var option = await Option.Some(42).TapAsync(value =>
        {
            tappedValue = value;

            return Task.CompletedTask;
        });

        tappedValue.ShouldBe(42);
        option.ShouldBe(Option.Some(42));
    }

    [Fact]
    public async Task when_tap_async_is_called_on_an_option_with_none_then_does_not_invoke_the_action()
    {
        bool actionInvoked = false;

        var option = await Option.None<int>().TapAsync(_ =>
        {
            actionInvoked = true;

            return Task.CompletedTask;
        });

        actionInvoked.ShouldBeFalse();
        option.ShouldBe(Option.None<int>());
    }

    [Fact]
    public async Task when_tap_async_is_called_on_a_task_of_option_with_a_sync_action_then_invokes_the_action_and_returns_the_original_option()
    {
        int tappedValue = 0;

        var option = await Task.FromResult(Option.Some(42)).TapAsync(value => tappedValue = value);

        tappedValue.ShouldBe(42);
        option.ShouldBe(Option.Some(42));
    }

    [Fact]
    public async Task when_tap_async_is_called_on_a_task_of_option_with_an_async_action_then_invokes_the_action_and_returns_the_original_option()
    {
        int tappedValue = 0;

        var option = await Task.FromResult(Option.Some(42)).TapAsync(value =>
        {
            tappedValue = value;

            return Task.CompletedTask;
        });

        tappedValue.ShouldBe(42);
        option.ShouldBe(Option.Some(42));
    }

    [Fact]
    public async Task when_match_async_is_called_with_an_async_on_some_and_the_option_is_some_then_invokes_on_some_async()
    {
        int actual = await Option.Some(42).MatchAsync(value => Task.FromResult(value * 2), () => -1);

        actual.ShouldBe(84);
    }

    [Fact]
    public async Task when_match_async_is_called_with_an_async_on_some_and_the_option_is_none_then_invokes_on_none()
    {
        int actual = await Option.None<int>().MatchAsync(value => Task.FromResult(value * 2), () => -1);

        actual.ShouldBe(-1);
    }

    [Fact]
    public async Task when_match_async_is_called_with_an_async_on_none_and_the_option_is_some_then_invokes_on_some()
    {
        int actual = await Option.Some(42).MatchAsync(value => value * 2, () => Task.FromResult(-1));

        actual.ShouldBe(84);
    }

    [Fact]
    public async Task when_match_async_is_called_with_an_async_on_none_and_the_option_is_none_then_invokes_on_none_async()
    {
        int actual = await Option.None<int>().MatchAsync(value => value * 2, () => Task.FromResult(-1));

        actual.ShouldBe(-1);
    }

    [Fact]
    public async Task when_match_async_is_called_with_both_async_functions_and_the_option_is_some_then_invokes_on_some_async()
    {
        int actual = await Option.Some(42).MatchAsync(value => Task.FromResult(value * 2), () => Task.FromResult(-1));

        actual.ShouldBe(84);
    }

    [Fact]
    public async Task when_match_async_is_called_with_both_async_functions_and_the_option_is_none_then_invokes_on_none_async()
    {
        int actual = await Option.None<int>().MatchAsync(value => Task.FromResult(value * 2), () => Task.FromResult(-1));

        actual.ShouldBe(-1);
    }
}
