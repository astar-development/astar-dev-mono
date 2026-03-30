using AStar.Dev.OneDriveSync.Infrastructure.Startup;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Startup;

public sealed class GivenAStartupOrchestrator
{
    private readonly ILogger<StartupOrchestrator> _logger = Substitute.For<ILogger<StartupOrchestrator>>();

    [Fact]
    public async Task when_all_tasks_succeed_then_all_results_have_succeeded_true()
    {
        var taskA = Substitute.For<IStartupTask>();
        var taskB = Substitute.For<IStartupTask>();
        taskA.Name.Returns("TaskA");
        taskB.Name.Returns("TaskB");
        taskA.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        taskB.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = new StartupOrchestrator([taskA, taskB], _logger);

        var results = await sut.RunAsync(CancellationToken.None);

        results.ShouldAllBe(r => r.Succeeded);
    }

    [Fact]
    public async Task when_a_task_throws_then_its_result_has_succeeded_false()
    {
        var failingTask = Substitute.For<IStartupTask>();
        failingTask.Name.Returns("Failing");
        failingTask.RunAsync(Arg.Any<CancellationToken>())
                   .Returns<Task>(_ => Task.FromException(new InvalidOperationException("boom")));
        var sut = new StartupOrchestrator([failingTask], _logger);

        var results = await sut.RunAsync(CancellationToken.None);

        results.Single().Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_task_throws_then_its_result_captures_the_exception()
    {
        var expectedException = new InvalidOperationException("boom");
        var failingTask = Substitute.For<IStartupTask>();
        failingTask.Name.Returns("Failing");
        failingTask.RunAsync(Arg.Any<CancellationToken>())
                   .Returns<Task>(_ => Task.FromException(expectedException));
        var sut = new StartupOrchestrator([failingTask], _logger);

        var results = await sut.RunAsync(CancellationToken.None);

        results.Single().Error.ShouldBe(expectedException);
    }

    [Fact]
    public async Task when_cancellation_is_requested_then_operation_cancelled_exception_propagates()
    {
        using var cts = new CancellationTokenSource();
        var cancellingTask = Substitute.For<IStartupTask>();
        cancellingTask.Name.Returns("Cancellable");
        cancellingTask.RunAsync(Arg.Any<CancellationToken>())
                      .Returns<Task>(_ => Task.FromException(new OperationCanceledException()));
        var sut = new StartupOrchestrator([cancellingTask], _logger);

        await Should.ThrowAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));
    }

    [Fact]
    public async Task when_multiple_tasks_are_registered_then_a_result_is_returned_for_each()
    {
        var tasks = Enumerable.Range(1, 5).Select(i =>
        {
            var t = Substitute.For<IStartupTask>();
            t.Name.Returns($"Task{i}");
            t.RunAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            return t;
        }).ToArray();
        var sut = new StartupOrchestrator(tasks, _logger);

        var results = await sut.RunAsync(CancellationToken.None);

        results.Count.ShouldBe(5);
    }
}
