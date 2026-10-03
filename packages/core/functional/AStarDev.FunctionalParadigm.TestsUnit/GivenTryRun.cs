namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenTryRun
{
    [Fact]
    public void when_operation_succeeds_then_returns_success_with_value()
    {
        var actual = Try.Run(() => 42);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(42));
    }

    [Fact]
    public void when_operation_throws_then_returns_failure_with_exception()
    {
        var exception = new InvalidOperationException("boom");

        var actual = Try.Run<int>(() => throw exception);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }

    [Fact]
    public void when_operation_throws_operation_canceled_exception_then_it_is_rethrown() =>
        Should.Throw<OperationCanceledException>(() => Try.Run<int>(() => throw new OperationCanceledException()));

    [Fact]
    public void when_operation_throws_task_canceled_exception_then_it_is_rethrown() =>
        Should.Throw<TaskCanceledException>(() => Try.Run<int>(() => throw new TaskCanceledException()));

    [Fact]
    public async Task when_awaited_task_is_canceled_then_operation_canceled_exception_propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Task<int> Operation() => Task.FromCanceled<int>(cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(() => Try.RunAsync(Operation));
    }
}
