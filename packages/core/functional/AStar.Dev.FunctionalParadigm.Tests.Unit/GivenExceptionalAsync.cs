using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalAsync
{
    [Fact]
    public async Task when_map_async_task_selector_and_success_then_transforms_value()
    {
        var exceptional = Exceptional.Success(5);

        var actual = await exceptional.MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(10));
    }

    [Fact]
    public async Task when_map_async_value_task_selector_and_success_then_transforms_value()
    {
        var exceptional = Exceptional.Success(5);

        var actual = await exceptional.MapAsync(value => ValueTask.FromResult(value * 2));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(10));
    }

    [Fact]
    public async Task when_map_async_and_failure_then_returns_same_failure()
    {
        var exception = new InvalidOperationException("bad");
        var exceptional = Exceptional.Failure<int>(exception);

        var actual = await exceptional.MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }

    [Fact]
    public async Task when_bind_async_and_success_then_returns_bound_result()
    {
        var exceptional = Exceptional.Success(3);

        var actual = await exceptional.BindAsync(value => Task.FromResult(Exceptional.Success(value * 4)));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(12));
    }

    [Fact]
    public async Task when_bind_async_and_failure_then_binder_not_invoked()
    {
        var exception = new InvalidOperationException("fail");
        var exceptional = Exceptional.Failure<int>(exception);
        var invoked = false;

        var actual = await exceptional.BindAsync(value =>
        {
            invoked = true;

            return Task.FromResult(Exceptional.Success(value));
        });

        actual.ShouldBeOfType<Failure<int>>();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_tap_async_on_task_of_success_then_executes_handler_and_returns_same()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(9));
        var sideEffect = false;

        var actual = await exceptionalTask.TapAsync(value => sideEffect = value == 9);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(9));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_async_on_task_of_failure_then_executes_failure_handler()
    {
        var exception = new InvalidOperationException("boom");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));
        var sideEffect = false;

        var actual = await exceptionalTask.TapAsync(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_match_async_on_success_then_invokes_async_success_handler()
    {
        var exceptional = Exceptional.Success(3);

        var actual = await exceptional.MatchAsync(onSuccess: value => Task.FromResult(value + 1), onFailure: _ => -1);

        actual.ShouldBe(4);
    }

    [Fact]
    public async Task when_match_async_on_failure_then_invokes_sync_failure_handler()
    {
        var exception = new InvalidOperationException("err");
        var exceptional = Exceptional.Failure<int>(exception);

        var actual = await exceptional.MatchAsync(onSuccess: value => Task.FromResult(value + 1), onFailure: ex => ex.Message.Length);

        actual.ShouldBe(3);
    }

    [Fact]
    public async Task when_map_async_on_task_receiver_with_sync_selector_and_success_then_transforms_value()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(5));

        var actual = await exceptionalTask.MapAsync(value => value * 2);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(10));
    }

    [Fact]
    public async Task when_map_async_on_task_receiver_with_sync_selector_and_failure_then_returns_same_failure()
    {
        var exception = new InvalidOperationException("bad");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));

        var actual = await exceptionalTask.MapAsync(value => value * 2);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }

    [Fact]
    public async Task when_bind_async_on_task_receiver_with_task_binder_and_success_then_returns_bound_result()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(3));

        var actual = await exceptionalTask.BindAsync(value => Task.FromResult(Exceptional.Success(value * 4)));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(12));
    }

    [Fact]
    public async Task when_bind_async_on_task_receiver_with_task_binder_and_failure_then_binder_not_invoked()
    {
        var exception = new InvalidOperationException("fail");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));
        var invoked = false;

        var actual = await exceptionalTask.BindAsync(value =>
        {
            invoked = true;

            return Task.FromResult(Exceptional.Success(value));
        });

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_bind_async_on_task_receiver_with_value_task_binder_and_success_then_returns_bound_result()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(2));

        var actual = await exceptionalTask.BindAsync(value => ValueTask.FromResult(Exceptional.Success(value + 5)));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(7));
    }

    [Fact]
    public async Task when_bind_async_on_task_receiver_with_value_task_binder_and_failure_then_binder_not_invoked()
    {
        var exception = new InvalidOperationException("fail");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));
        var invoked = false;

        var actual = await exceptionalTask.BindAsync(value =>
        {
            invoked = true;

            return ValueTask.FromResult(Exceptional.Success(value));
        });

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_bind_async_on_value_task_receiver_with_task_binder_and_success_then_returns_bound_result()
    {
        var exceptionalTask = ValueTask.FromResult(Exceptional.Success(6));

        var actual = await exceptionalTask.BindAsync(value => Task.FromResult(Exceptional.Success(value - 1)));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(5));
    }

    [Fact]
    public async Task when_bind_async_on_value_task_receiver_with_task_binder_and_failure_then_binder_not_invoked()
    {
        var exception = new InvalidOperationException("fail");
        var exceptionalTask = ValueTask.FromResult(Exceptional.Failure<int>(exception));
        var invoked = false;

        var actual = await exceptionalTask.BindAsync(value =>
        {
            invoked = true;

            return Task.FromResult(Exceptional.Success(value));
        });

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_bind_async_on_value_task_receiver_with_value_task_binder_and_success_then_returns_bound_result()
    {
        var exceptionalTask = ValueTask.FromResult(Exceptional.Success(4));

        var actual = await exceptionalTask.BindAsync(value => ValueTask.FromResult(Exceptional.Success(value + 3)));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(7));
    }

    [Fact]
    public async Task when_bind_async_on_value_task_receiver_with_value_task_binder_and_failure_then_binder_not_invoked()
    {
        var exception = new InvalidOperationException("fail");
        var exceptionalTask = ValueTask.FromResult(Exceptional.Failure<int>(exception));
        var invoked = false;

        var actual = await exceptionalTask.BindAsync(value =>
        {
            invoked = true;

            return ValueTask.FromResult(Exceptional.Success(value));
        });

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_tap_on_task_of_success_then_executes_handler_and_returns_same()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(9));
        var sideEffect = false;

        var actual = await exceptionalTask.Tap(value => sideEffect = value == 9);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(9));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_on_task_of_failure_then_executes_failure_handler()
    {
        var exception = new InvalidOperationException("boom");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));
        var sideEffect = false;

        var actual = await exceptionalTask.Tap(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_on_value_task_of_success_then_executes_handler_and_returns_same()
    {
        var exceptionalTask = ValueTask.FromResult(Exceptional.Success(11));
        var sideEffect = false;

        var actual = await exceptionalTask.Tap(value => sideEffect = value == 11);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(11));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_on_value_task_of_failure_then_executes_failure_handler()
    {
        var exception = new InvalidOperationException("boom");
        var exceptionalTask = ValueTask.FromResult(Exceptional.Failure<int>(exception));
        var sideEffect = false;

        var actual = await exceptionalTask.Tap(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_async_on_value_task_of_success_then_executes_handler_and_returns_same()
    {
        var exceptionalTask = ValueTask.FromResult(Exceptional.Success(13));
        var sideEffect = false;

        var actual = await exceptionalTask.TapAsync(value => sideEffect = value == 13);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(13));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_async_on_value_task_of_failure_then_executes_failure_handler()
    {
        var exception = new InvalidOperationException("boom");
        var exceptionalTask = ValueTask.FromResult(Exceptional.Failure<int>(exception));
        var sideEffect = false;

        var actual = await exceptionalTask.TapAsync(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_match_async_on_task_receiver_with_sync_handlers_and_success_then_invokes_success_handler()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(3));

        var actual = await exceptionalTask.MatchAsync(onSuccess: value => value + 1, onFailure: _ => -1);

        actual.ShouldBe(4);
    }

    [Fact]
    public async Task when_match_async_on_task_receiver_with_sync_handlers_and_failure_then_invokes_failure_handler()
    {
        var exception = new InvalidOperationException("err");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));

        var actual = await exceptionalTask.MatchAsync(onSuccess: value => value + 1, onFailure: ex => ex.Message.Length);

        actual.ShouldBe(3);
    }
}
