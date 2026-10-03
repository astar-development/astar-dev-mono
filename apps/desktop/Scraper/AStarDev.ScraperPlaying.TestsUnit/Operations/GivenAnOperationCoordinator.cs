using AStarDev.ScraperPlaying.Operations;

namespace AStarDev.ScraperPlaying.TestsUnit.Operations;

public sealed class GivenAnOperationCoordinator
{
    [Fact]
    public void when_an_operation_starts_then_another_cannot_start_until_it_completes()
    {
        using var coordinator = new OperationCoordinator();

        coordinator.TryStart(out _).ShouldBeTrue();
        coordinator.IsOperationRunning.ShouldBeTrue();
        coordinator.TryStart(out _).ShouldBeFalse();

        coordinator.Complete();

        coordinator.IsOperationRunning.ShouldBeFalse();
        coordinator.TryStart(out _).ShouldBeTrue();
    }

    [Fact]
    public void when_the_current_operation_is_cancelled_then_its_token_is_cancelled()
    {
        using var coordinator = new OperationCoordinator();
        coordinator.TryStart(out var cancellationToken);

        coordinator.Cancel();

        cancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_a_cancelled_operation_completes_then_the_next_operation_receives_a_fresh_token()
    {
        using var coordinator = new OperationCoordinator();
        coordinator.TryStart(out var cancelledToken);
        coordinator.Cancel();
        coordinator.Complete();

        coordinator.TryStart(out var nextToken).ShouldBeTrue();

        cancelledToken.IsCancellationRequested.ShouldBeTrue();
        nextToken.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void when_an_operation_starts_and_completes_then_state_changes_are_reported()
    {
        using var coordinator = new OperationCoordinator();
        var stateChangeCount = 0;
        coordinator.StateChanged += (_, _) => stateChangeCount++;

        coordinator.TryStart(out _);
        coordinator.Complete();

        stateChangeCount.ShouldBe(2);
    }

    [Fact]
    public void when_disposed_multiple_times_then_no_exception_is_thrown()
    {
        using var coordinator = new OperationCoordinator();

        Should.NotThrow(coordinator.Dispose);
        Should.NotThrow(coordinator.Dispose);
    }

    [Fact]
    public void when_cancel_is_called_after_the_operation_completed_then_no_exception_is_thrown()
    {
        using var coordinator = new OperationCoordinator();
        coordinator.TryStart(out _);
        coordinator.Complete();

        Should.NotThrow(coordinator.Cancel);
    }

    [Fact]
    public async Task when_cancel_and_complete_race_then_no_exception_is_thrown()
    {
        using var coordinator = new OperationCoordinator();

        await Should.NotThrowAsync(() => Task.Run(() => Parallel.For(0, 20_000, iteration =>
        {
            coordinator.TryStart(out _);
            Parallel.Invoke(coordinator.Cancel, coordinator.Complete);
        })));
    }
}
