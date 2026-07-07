using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenBindAsync
{
    [Fact]
    public async Task when_binder_returns_success_task_then_returns_bound_success()
    {
        var result = Result.Success<int, string>(3);

        var actual = await result.BindAsync(value => Task.FromResult(Result.Success<int, string>(value * 4)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(12));
    }

    [Fact]
    public async Task when_result_is_failure_then_returns_failure_without_invoking_binder()
    {
        var result = Result.Failure<int, string>("fail");
        var invoked = false;

        var actual = await result.BindAsync(value =>
        {
            invoked = true;
            return Task.FromResult(Result.Success<int, string>(value * 4));
        });

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("fail"));
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_binder_returns_value_task_then_returns_bound_success()
    {
        var result = Result.Success<int, string>(2);

        var actual = await result.BindAsync(value => ValueTask.FromResult(Result.Success<int, string>(value + 5)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(7));
    }

    [Fact]
    public async Task when_result_task_and_binder_returns_task_then_returns_bound_success()
    {
        var resultTask = Task.FromResult<Result<int, string>>(Result.Success<int, string>(5));

        var actual = await resultTask.BindAsync(value => Task.FromResult(Result.Success<int, string>(value * 2)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(10));
    }

    [Fact]
    public async Task when_value_task_result_and_binder_returns_value_task_then_returns_bound_success()
    {
        var resultTask = ValueTask.FromResult<Result<int, string>>(Result.Success<int, string>(4));

        var actual = await resultTask.BindAsync(value => ValueTask.FromResult(Result.Success<int, string>(value + 3)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(7));
    }

    [Fact]
    public async Task when_task_result_failure_then_binder_not_invoked()
    {
        var resultTask = Task.FromResult<Result<int, string>>(Result.Failure<int, string>("err"));
        var invoked = false;

        var actual = await resultTask.BindAsync(value =>
        {
            invoked = true;
            return Task.FromResult(Result.Success<int, string>(value));
        });

        actual.ShouldBeOfType<Fail<int, string>>();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_task_result_with_value_task_binder_then_returns_bound_success()
    {
        var resultTask = Task.FromResult<Result<int, string>>(Result.Success<int, string>(3));

        var actual = await resultTask.BindAsync(value => ValueTask.FromResult(Result.Success<int, string>(value * 10)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(30));
    }

    [Fact]
    public async Task when_task_result_failure_with_value_task_binder_then_returns_failure()
    {
        var resultTask = Task.FromResult<Result<int, string>>(Result.Failure<int, string>("failed"));

        var actual = await resultTask.BindAsync(value => ValueTask.FromResult(Result.Success<int, string>(value)));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("failed"));
    }

    [Fact]
    public async Task when_value_task_result_with_task_binder_then_returns_bound_success()
    {
        var resultTask = ValueTask.FromResult<Result<int, string>>(Result.Success<int, string>(6));

        var actual = await resultTask.BindAsync(value => Task.FromResult(Result.Success<int, string>(value - 1)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(5));
    }

    [Fact]
    public async Task when_value_task_result_failure_with_task_binder_then_returns_failure()
    {
        var resultTask = ValueTask.FromResult<Result<int, string>>(Result.Failure<int, string>("error"));

        var actual = await resultTask.BindAsync(value => Task.FromResult(Result.Success<int, string>(value)));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("error"));
    }

    [Fact]
    public async Task when_binder_returns_failure_then_returns_that_failure()
    {
        var result = Result.Success<int, string>(3);

        var actual = await result.BindAsync(value => Task.FromResult(Result.Failure<int, string>($"failed-{value}")));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("failed-3"));
    }

    [Fact]
    public async Task when_chaining_multiple_bind_async_calls_then_passes_through_all()
    {
        var result = Result.Success<int, string>(2);

        var intermediate = await result.BindAsync(v => Task.FromResult(Result.Success<int, string>(v * 2)));
        var actual = await intermediate.BindAsync(v => ValueTask.FromResult(Result.Success<int, string>(v + 1)));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(5)); // (2 * 2) + 1
    }
}
