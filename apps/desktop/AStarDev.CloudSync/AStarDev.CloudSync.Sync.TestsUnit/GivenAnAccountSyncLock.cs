using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnAccountSyncLock : IDisposable
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId Work = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly ServiceProvider provider = new ServiceCollection().AddCloudSync().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    [Fact]
    public void when_no_pass_is_running_then_the_lease_is_acquired_for_the_account()
    {
        var attempt = SyncLock.TryAcquire(Personal);

        using var lease = attempt.ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        lease.AccountId.ShouldBe(Personal);
    }

    [Fact]
    public void when_a_pass_is_running_then_the_account_is_busy_with_that_pass()
    {
        using var running = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Personal)).Lease;

        var attempt = SyncLock.TryAcquire(Personal);

        attempt.ShouldBe(SyncLockAttemptFactory.CreateBusy(running.PassId));
    }

    [Fact]
    public void when_another_account_is_syncing_then_this_account_can_still_start()
    {
        using var running = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Work)).Lease;

        var attempt = SyncLock.TryAcquire(Personal);

        using var lease = attempt.ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        lease.PassId.ShouldNotBe(running.PassId);
    }

    [Fact]
    public void when_the_lease_is_disposed_then_the_next_pass_can_start()
    {
        ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Personal)).Lease.Dispose();

        var attempt = SyncLock.TryAcquire(Personal);

        using var lease = attempt.ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
    }

    [Fact]
    public void when_an_old_lease_is_disposed_again_then_the_newer_pass_keeps_its_lease()
    {
        var old = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Personal)).Lease;
        old.Dispose();
        using var current = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Personal)).Lease;

        old.Dispose();

        SyncLock.TryAcquire(Personal).ShouldBe(SyncLockAttemptFactory.CreateBusy(current.PassId));
    }

    [Fact]
    public async Task when_many_passes_start_at_once_for_one_account_then_exactly_one_is_acquired()
    {
        var attempts = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => SyncLock.TryAcquire(Personal), CancellationToken)));

        attempts.OfType<SyncLockAttempt.Acquired>().Count().ShouldBe(1);
        attempts.Where(attempt => attempt is not SyncLockAttempt.Acquired).ShouldAllBe(attempt => attempt is SyncLockAttempt.Busy);
        attempts.OfType<SyncLockAttempt.Acquired>().Single().Lease.Dispose();
    }

    [Fact]
    public async Task when_nothing_is_running_then_a_drain_completes_at_once_and_blocks_new_passes()
    {
        using var drain = await SyncLock.DrainAsync(CancellationToken);

        SyncLock.TryAcquire(Personal).ShouldBe(SyncLockAttemptFactory.CreateDraining());
    }

    [Fact]
    public async Task when_the_drain_is_disposed_then_passes_can_start_again()
    {
        var drain = await SyncLock.DrainAsync(CancellationToken);

        drain.Dispose();

        using var lease = SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
    }

    [Fact]
    public async Task when_passes_are_running_then_the_drain_waits_until_every_lease_is_released()
    {
        var personal = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Personal)).Lease;
        var work = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Work)).Lease;

        var draining = SyncLock.DrainAsync(CancellationToken);
        personal.Dispose();
        var stillWaiting = !draining.IsCompleted;
        work.Dispose();
        using var drain = await draining.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        stillWaiting.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_drain_is_cancelled_while_waiting_then_it_throws_and_the_lock_reopens()
    {
        using var running = ((SyncLockAttempt.Acquired)SyncLock.TryAcquire(Work)).Lease;
        using var cancellation = new CancellationTokenSource();

        var draining = SyncLock.DrainAsync(cancellation.Token);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => draining);
        using var lease = SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
    }

    [Fact]
    public async Task when_two_drains_are_held_then_disposing_one_keeps_the_lock_closed()
    {
        var first = await SyncLock.DrainAsync(CancellationToken);
        using var second = await SyncLock.DrainAsync(CancellationToken);

        first.Dispose();
        first.Dispose();

        SyncLock.TryAcquire(Personal).ShouldBe(SyncLockAttemptFactory.CreateDraining());
    }

    public void Dispose() => provider.Dispose();
}
