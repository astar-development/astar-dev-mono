namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenTryRunAsync
{
    [Fact]
    public async Task when_operation_succeeds_then_returns_success_with_value()
    {
        var actual = await Try.RunAsync(() => Task.FromResult(42));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(42));
    }

    [Fact]
    public async Task when_operation_throws_then_returns_failure_with_exception()
    {
        var exception = new InvalidOperationException("async boom");

        var actual = await Try.RunAsync<int>(() => throw exception);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }

    [Fact]
    public async Task when_operation_throws_operation_canceled_exception_then_it_is_rethrown() =>
        await Should.ThrowAsync<OperationCanceledException>(() => Try.RunAsync<int>(() => throw new OperationCanceledException()));

    [Fact]
    public async Task when_operation_throws_task_canceled_exception_then_it_is_rethrown() =>
        await Should.ThrowAsync<TaskCanceledException>(() => Try.RunAsync<int>(() => throw new TaskCanceledException()));

    [Fact]
    public async Task when_awaited_task_is_canceled_then_operation_canceled_exception_propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Task<int> Operation() => Task.FromCanceled<int>(cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(() => Try.RunAsync(Operation));
    }

    [Fact]
    public async Task when_cancellation_token_is_already_cancelled_then_operation_canceled_exception_propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => Try.RunAsync(() => Task.FromResult(1), cts.Token));
    }

    [Fact]
    public async Task when_cancellation_token_is_not_cancelled_then_operation_result_is_captured()
    {
        using var cts = new CancellationTokenSource();

        var actual = await Try.RunAsync(() => Task.FromResult(9), cts.Token);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(9));
    }
}
