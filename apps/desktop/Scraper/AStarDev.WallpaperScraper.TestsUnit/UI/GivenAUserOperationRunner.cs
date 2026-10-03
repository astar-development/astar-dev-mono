using AStarDev.WallpaperScraper.Operations;
using System.Text.Json;
using AStarDev.WallpaperScraper.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenAUserOperationRunner : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly UserOperationRunner runner;

    public GivenAUserOperationRunner() => runner = new(coordinator, status);

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_the_operation_succeeds_then_it_runs_with_the_operation_marked_as_running_and_completes_afterwards()
    {
        var wasRunning = false;

        await runner.RunAsync("cancelled", "failed", _ =>
        {
            wasRunning = coordinator.IsOperationRunning;

            return Task.CompletedTask;
        });

        (wasRunning, coordinator.IsOperationRunning, status.Text).ShouldBe((true, false, string.Empty));
    }

    [Fact]
    public async Task when_another_operation_is_already_running_then_the_operation_is_not_run()
    {
        _ = coordinator.TryStart(out _);
        var ran = false;

        await runner.RunAsync("cancelled", "failed", _ =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_then_the_cancelled_message_is_reported_and_the_operation_completes()
    {
        await runner.RunAsync("Import cancelled.", "failed", async cancellationToken =>
        {
            runner.Cancel();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        });

        (status.Text, coordinator.IsOperationRunning).ShouldBe(($"{TestStatusReporter.Timestamp} Import cancelled.", false));
    }

    [Theory]
    [MemberData(nameof(ExpectedFailures))]
    public async Task when_the_operation_fails_with_an_expected_exception_then_the_failure_is_reported_and_the_operation_completes(Exception failure)
    {
        await runner.RunAsync("cancelled", "Unable to import.", _ => throw failure);

        (status.Text, coordinator.IsOperationRunning).ShouldBe(($"{TestStatusReporter.Timestamp} Unable to import. {failure.Message}", false));
    }

    [Fact]
    public async Task when_the_operation_fails_with_an_exception_that_is_not_cancellation_then_it_is_reported_not_thrown_so_it_cannot_escape_an_async_void_handler()
    {
        await Should.NotThrowAsync(() => runner.RunAsync("cancelled", "Unable to export.", _ => throw new InvalidCastException("unexpected")));

        (status.Text, coordinator.IsOperationRunning).ShouldBe(($"{TestStatusReporter.Timestamp} Unable to export. unexpected", false));
    }

    [Fact]
    public async Task when_an_exception_is_reported_with_an_inner_exception_then_the_cause_is_included()
    {
        await runner.RunAsync("cancelled", "Unable to import.", _ => throw new UnauthorizedAccessException("Access denied", new IOException("read-only file system")));

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to import. Access denied Caused by: read-only file system");
    }

    [Fact]
    public async Task when_a_guarded_operation_succeeds_then_nothing_is_reported()
    {
        var ran = false;

        await runner.ReportFailuresAsync("Unable to edit.", () =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        (ran, status.Text).ShouldBe((true, string.Empty));
    }

    [Fact]
    public async Task when_a_guarded_operation_fails_then_the_failure_is_reported_not_thrown()
    {
        await Should.NotThrowAsync(() => runner.ReportFailuresAsync("Unable to edit.", () => throw new InvalidOperationException("dialog failed")));

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Unable to edit. dialog failed");
    }

    [Fact]
    public async Task when_a_guarded_operation_runs_while_another_operation_is_running_then_it_still_runs()
    {
        _ = coordinator.TryStart(out _);
        var ran = false;

        await runner.ReportFailuresAsync("Unable to edit.", () =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        ran.ShouldBeTrue();
    }

    public static TheoryData<Exception> ExpectedFailures =>
    [
        new IOException("disk"),
        new JsonException("json"),
        new InvalidOperationException("invalid"),
        new UnauthorizedAccessException("access denied"),
        new ArgumentNullException("document", "value cannot be null")
    ];
}
