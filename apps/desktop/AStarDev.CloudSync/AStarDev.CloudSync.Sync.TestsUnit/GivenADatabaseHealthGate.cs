using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADatabaseHealthGate
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly ScriptedDatabaseHealthCheck check = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IDatabaseHealthGate CreateGate() => new ServiceCollection().AddSingleton<IDatabaseHealthCheck>(check).AddCloudSync().BuildServiceProvider().GetRequiredService<IDatabaseHealthGate>();

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

        var status = await gate.CheckAsync(CancellationToken);

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
}
