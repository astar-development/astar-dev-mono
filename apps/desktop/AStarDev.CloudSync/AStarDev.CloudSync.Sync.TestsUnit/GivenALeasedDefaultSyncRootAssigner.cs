using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALeasedDefaultSyncRootAssigner : IDisposable
{
    private const string Personal = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string Work = "00000000-0000-0000-f1e2-d3c4b5a69788.9188040d-6c67-4c5b-b112-36a304b66dad";

    private readonly WriteGate gate = new();
    private readonly AccountServices services;

    public GivenALeasedDefaultSyncRootAssigner() => services = new AccountServices(gate: gate);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static AccountRegistration Registration() => AccountFixtures.Registration(Personal);

    [Fact]
    public async Task when_the_account_is_idle_then_the_default_root_is_saved()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        gate.Release();

        var result = await services.RootAssigner.AssignAsync(accountId, Registration(), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        services.SyncRoots.Roots.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_assignment_ends_then_the_lease_is_released()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        gate.Release();
        _ = await services.RootAssigner.AssignAsync(accountId, Registration(), CancellationToken);

        var next = services.SyncLock.TryAcquire(accountId);

        next.ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    [Fact]
    public async Task when_a_pass_keeps_running_then_the_assignment_waits_ten_seconds_then_reports_sync_still_running_and_saves_nothing()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;
        gate.Release();

        var result = await services.AdvanceUntilAsync(services.RootAssigner.AssignAsync(accountId, Registration(), CancellationToken));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateSyncStillRunning());
        services.SyncRoots.SetAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_lock_is_drained_then_the_assignment_reports_sync_still_running_without_waiting()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var drain = await services.SyncLock.DrainAsync(CancellationToken);
        gate.Release();

        var result = await services.RootAssigner.AssignAsync(accountId, Registration(), CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateSyncStillRunning());
        services.SyncRoots.SetAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_another_account_is_busy_then_the_assignment_goes_ahead()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var otherId = await services.AddAsync(Work, CancellationToken);
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(otherId)).Lease;
        gate.Release();

        var result = await services.RootAssigner.AssignAsync(accountId, Registration(), CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_an_account_removal_starts_during_an_assignment_then_the_removal_waits_for_the_assignment()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var assigning = services.RootAssigner.AssignAsync(accountId, Registration(), CancellationToken);
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        var removing = services.Actions.RemoveAsync(accountId, CancellationToken);
        services.Time.Advance(TimeSpan.FromSeconds(2));
        await Task.Delay(50, CancellationToken);

        removing.IsCompleted.ShouldBeFalse();
        services.Steps.ShouldBeEmpty();

        gate.Release();
        await assigning;
        var removed = await services.AdvanceUntilAsync(removing);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
        services.SyncRoots.Roots.Count.ShouldBe(1);
    }

    public void Dispose() => services.Dispose();
}
