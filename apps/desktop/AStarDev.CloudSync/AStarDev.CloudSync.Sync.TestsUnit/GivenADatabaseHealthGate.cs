using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADatabaseHealthGate
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan RetryPause = TimeSpan.FromMilliseconds(250);

    private readonly ScriptedDatabaseHealthCheck check = new();
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IDatabaseHealthGate CreateGate() => new ServiceCollection().AddSingleton<System.TimeProvider>(time).AddSingleton<IDatabaseHealthCheck>(check).AddCloudSync().BuildServiceProvider().GetRequiredService<IDatabaseHealthGate>();

    private async Task<DatabaseHealthStatus> FinishedAsync(Task<DatabaseHealthStatus> checking)
    {
        while (!checking.IsCompleted)
        {
            time.Advance(RetryPause);
            await Task.Delay(5, CancellationToken);
        }

        return await checking;
    }

    [Fact]
    public void when_nothing_has_been_checked_then_the_status_is_not_checked()
    {
        var gate = CreateGate();

        gate.Status.ShouldBe(DatabaseHealthStatusFactory.CreateNotChecked());
    }

    [Fact]
    public async Task when_the_check_passes_then_the_status_is_healthy()
    {
        var gate = CreateGate();

        var status = await gate.CheckAsync(CancellationToken);

        (status, gate.Status).ShouldBe((DatabaseHealthStatusFactory.CreateHealthy(), DatabaseHealthStatusFactory.CreateHealthy()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task when_the_check_fails_then_the_status_carries_the_failure(bool damaged)
    {
        var failure = damaged ? DatabaseHealthFailureFactory.CreateDamaged() : DatabaseHealthFailureFactory.CreateUnreadable();
        check.Answer = Result.Failure<Unit, DatabaseHealthFailure>(failure);
        var gate = CreateGate();

        var status = await FinishedAsync(gate.CheckAsync(CancellationToken));

        status.ShouldBe(DatabaseHealthStatusFactory.CreateFailed(failure));
    }

    [Fact]
    public async Task when_many_callers_ask_at_once_then_the_check_runs_once_and_they_all_get_the_same_answer()
    {
        var held = new WriteGate();
        check.Gate = Option.Some(held);
        var gate = CreateGate();

        var callers = Enumerable.Range(0, 20).Select(_ => Task.Run(() => gate.CheckAsync(CancellationToken), CancellationToken)).ToArray();
        await held.Entered.WaitAsync(Limit, CancellationToken);
        held.Release();
        var answers = await Task.WhenAll(callers).WaitAsync(Limit, CancellationToken);

        (check.Calls, answers.Distinct().Count()).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_the_check_has_finished_then_a_later_call_does_not_run_it_again()
    {
        var gate = CreateGate();
        _ = await gate.CheckAsync(CancellationToken);

        _ = await gate.CheckAsync(CancellationToken);

        check.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_check_is_cancelled_then_nothing_is_remembered_and_the_next_call_checks_again()
    {
        var held = new WriteGate();
        check.Gate = Option.Some(held);
        var gate = CreateGate();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var cancelled = gate.CheckAsync(cancel.Token);
        await held.Entered.WaitAsync(Limit, CancellationToken);
        await cancel.CancelAsync();
        var cancelledStatus = await cancelled.WaitAsync(Limit, CancellationToken);
        held.Release();

        var status = await gate.CheckAsync(CancellationToken);

        (cancelledStatus, status, check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateNotChecked(), DatabaseHealthStatusFactory.CreateHealthy(), 2));
    }

    [Fact]
    public async Task when_the_check_finishes_then_changed_is_raised_once()
    {
        var gate = CreateGate();
        var raised = 0;
        gate.Changed += (_, _) => raised++;

        _ = await gate.CheckAsync(CancellationToken);
        _ = await gate.CheckAsync(CancellationToken);

        raised.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_check_throws_then_the_status_is_failed_unreadable_and_later_callers_are_not_blocked()
    {
        check.Throws = true;
        var gate = CreateGate();

        var first = await FinishedAsync(gate.CheckAsync(CancellationToken));
        var second = await gate.CheckAsync(CancellationToken).WaitAsync(Limit, CancellationToken);

        (first, second).ShouldBe((DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable()), DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable())));
    }

    [Fact]
    public async Task when_the_first_check_is_unreadable_and_the_second_passes_then_the_status_is_healthy()
    {
        check.Queued.Enqueue(Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable()));
        var gate = CreateGate();

        var status = await FinishedAsync(gate.CheckAsync(CancellationToken));

        (status, check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateHealthy(), 2));
    }

    [Fact]
    public async Task when_the_check_stays_unreadable_then_the_status_is_failed_after_one_retry()
    {
        check.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable());
        var gate = CreateGate();

        var status = await FinishedAsync(gate.CheckAsync(CancellationToken));

        (status, check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable()), 2));
    }

    [Fact]
    public async Task when_the_check_reports_damage_then_it_is_not_retried()
    {
        check.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateDamaged());
        var gate = CreateGate();

        var status = await gate.CheckAsync(CancellationToken);

        (status, check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateDamaged()), 1));
    }

    [Fact]
    public async Task when_many_callers_ask_during_a_transient_lock_then_all_get_healthy_and_the_failure_is_never_seen()
    {
        check.Queued.Enqueue(Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable()));
        var gate = CreateGate();
        var seen = new System.Collections.Concurrent.ConcurrentBag<DatabaseHealthStatus>();
        gate.Changed += (_, _) => seen.Add(gate.Status);

        var callers = Enumerable.Range(0, 20).Select(_ => Task.Run(() => gate.CheckAsync(CancellationToken), CancellationToken)).ToArray();
        var answers = await FinishedAllAsync(callers);

        (answers.Distinct().Single(), seen.Distinct().Single(), check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateHealthy(), DatabaseHealthStatusFactory.CreateHealthy(), 2));
    }

    private async Task<DatabaseHealthStatus[]> FinishedAllAsync(Task<DatabaseHealthStatus>[] callers)
    {
        var all = Task.WhenAll(callers);
        while (!all.IsCompleted)
        {
            time.Advance(RetryPause);
            await Task.Delay(5, CancellationToken);
        }

        return await all;
    }

    [Fact]
    public async Task when_the_first_attempt_is_unreadable_then_the_retry_waits_for_the_pause_before_it_runs()
    {
        check.Queued.Enqueue(Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable()));
        var gate = CreateGate();

        var checking = gate.CheckAsync(CancellationToken);
        while (check.Calls < 1)
            await Task.Delay(5, CancellationToken);
        await Task.Delay(50, CancellationToken);
        var callsBeforeThePause = check.Calls;
        var status = await FinishedAsync(checking);

        (callsBeforeThePause, check.Calls, status).ShouldBe((1, 2, DatabaseHealthStatusFactory.CreateHealthy()));
    }

    [Fact]
    public async Task when_the_lock_outlasts_the_retry_then_the_status_is_failed()
    {
        check.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable());
        var gate = CreateGate();

        var status = await FinishedAsync(gate.CheckAsync(CancellationToken));

        (status, check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable()), 2));
    }

    [Fact]
    public async Task when_the_check_is_cancelled_during_the_pause_then_nothing_is_remembered()
    {
        check.Queued.Enqueue(Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable()));
        var gate = CreateGate();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var checking = gate.CheckAsync(cancel.Token);
        while (check.Calls < 1)
            await Task.Delay(5, CancellationToken);
        await Task.Delay(50, CancellationToken);

        await cancel.CancelAsync();
        var status = await checking.WaitAsync(Limit, CancellationToken);

        (status, gate.Status, check.Calls).ShouldBe((DatabaseHealthStatusFactory.CreateNotChecked(), DatabaseHealthStatusFactory.CreateNotChecked(), 1));
    }

    [Fact]
    public async Task when_the_gate_is_disposed_while_the_check_runs_then_the_caller_still_gets_an_answer()
    {
        var held = new WriteGate();
        check.Gate = Option.Some(held);
        var gate = CreateGate();
        var checking = gate.CheckAsync(CancellationToken);
        await held.Entered.WaitAsync(Limit, CancellationToken);

        ((IDisposable)gate).Dispose();
        held.Release();
        var status = await checking.WaitAsync(Limit, CancellationToken);

        status.ShouldBe(DatabaseHealthStatusFactory.CreateHealthy());
    }
}
