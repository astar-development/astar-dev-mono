using AStar.Dev.Sync.Engine.Features.Resilience;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.Resilience;

public sealed class GivenExponentialBackoff
{
    private readonly ExponentialBackoffPolicy _sut = new(NullLogger<ExponentialBackoffPolicy>.Instance);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(5, 32)]
    [InlineData(6, 60)]
    [InlineData(10, 60)]
    public void when_attempt_is_n_then_delay_is_min_of_2_to_the_n_and_60_seconds(int attempt, double expectedSeconds)
    {
        var delay = ExponentialBackoffPolicy.CalculateDelay(attempt);

        delay.TotalSeconds.ShouldBe(expectedSeconds);
    }

    [Fact]
    public async Task when_operation_succeeds_on_first_attempt_then_no_retry_occurs()
    {
        var callCount = 0;
        var ct = TestContext.Current.CancellationToken;

        await _sut.ExecuteAsync(_ =>
        {
            callCount++;

            return Task.CompletedTask;
        }, ct);

        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_cancellation_is_requested_before_start_then_operation_is_not_called()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var callCount = 0;

#pragma warning disable xUnit1051
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _sut.ExecuteAsync(_ =>
            {
                callCount++;

                return Task.CompletedTask;
            }, cts.Token));
#pragma warning restore xUnit1051

        callCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_cancellation_is_requested_during_retry_then_retries_stop()
    {
        using var cts = new CancellationTokenSource();

        var callCount = 0;

#pragma warning disable xUnit1051
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _sut.ExecuteAsync(async innerCt =>
            {
                callCount++;
                await cts.CancelAsync();
                throw new InvalidOperationException("transient");
            }, cts.Token));
#pragma warning restore xUnit1051

        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_operation_returns_a_value_then_result_is_returned()
    {
        const int expectedValue = 42;
        var ct = TestContext.Current.CancellationToken;

        var result = await _sut.ExecuteAsync<int>(_ => Task.FromResult(expectedValue), ct);

        result.ShouldBe(expectedValue);
    }
}
