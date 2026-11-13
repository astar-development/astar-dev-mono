using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALeasedSelectionRuleWriter : IDisposable
{
    private const string Personal = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string Work = "00000000-0000-0000-f1e2-d3c4b5a69788.9188040d-6c67-4c5b-b112-36a304b66dad";

    private readonly WriteGate gate = new();
    private readonly AccountServices services;

    public GivenALeasedSelectionRuleWriter() => services = new AccountServices(gate: gate);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static IReadOnlyList<SelectionChange> Changes() => [SelectionChangeFactory.CreateIncludeFolder(Path("Docs"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay())];

    [Fact]
    public async Task when_the_account_is_idle_then_the_changes_are_saved()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        gate.Release();

        var result = await services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        services.Rules.Applied.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_save_ends_then_the_lease_is_released()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        gate.Release();
        _ = await services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken);

        var next = services.SyncLock.TryAcquire(accountId);

        next.ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    [Fact]
    public async Task when_the_save_fails_then_the_lease_is_released()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.Rules.Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("save failed"));
        gate.Release();

        var result = await services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken);

        result.Match(_ => false, _ => true).ShouldBeTrue();
        services.SyncLock.TryAcquire(accountId).ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    [Fact]
    public async Task when_a_pass_keeps_running_then_the_save_waits_ten_seconds_then_fails_and_saves_nothing()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;
        gate.Release();

        var result = await services.AdvanceUntilAsync(services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken));

        result.Match(_ => false, _ => true).ShouldBeTrue();
        services.Rules.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_pass_ends_within_ten_seconds_then_the_save_goes_ahead()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;
        gate.Release();

        var saving = services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken);
        services.Time.Advance(TimeSpan.FromSeconds(3));
        running.Dispose();
        var result = await services.AdvanceUntilAsync(saving);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        services.Rules.Applied.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_lock_is_drained_then_the_save_fails_without_waiting_and_saves_nothing()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var drain = await services.SyncLock.DrainAsync(CancellationToken);
        gate.Release();

        var result = await services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        result.Match(_ => false, _ => true).ShouldBeTrue();
        services.Rules.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_another_account_is_busy_then_the_save_goes_ahead()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var otherId = await services.AddAsync(Work, CancellationToken);
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(otherId)).Lease;
        gate.Release();

        var result = await services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_an_account_removal_starts_during_a_save_then_the_removal_waits_for_the_save()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var saving = services.LeasedRules.ApplyAsync(accountId, Changes(), CancellationToken);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        var removing = services.Actions.RemoveAsync(accountId, CancellationToken);
        services.Time.Advance(TimeSpan.FromSeconds(2));
        await Task.Delay(50, CancellationToken);

        removing.IsCompleted.ShouldBeFalse();
        services.Steps.ShouldBeEmpty();

        gate.Release();
        await saving;
        var removed = await services.AdvanceUntilAsync(removing);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
        services.Rules.Applied.Count.ShouldBe(1);
    }

    public void Dispose() => services.Dispose();
}
