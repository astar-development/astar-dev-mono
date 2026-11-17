using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenABackgroundLease : IDisposable
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId Work = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly FakeTimeProvider time = new();
    private readonly ServiceProvider provider;

    public GivenABackgroundLease() => provider = new ServiceCollection().AddSingleton<TimeProvider>(time).AddCloudSync().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    private SyncLease Background(AccountId accountId) => SyncLock.TryAcquireBackground(accountId).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;

    [Fact]
    public void when_nothing_holds_the_account_then_a_background_lease_is_acquired()
    {
        using var lease = Background(Personal);

        lease.AccountId.ShouldBe(Personal);
    }

    [Fact]
    public void when_a_background_lease_is_held_then_a_one_shot_attempt_is_busy_with_it()
    {
        using var background = Background(Personal);

        SyncLock.TryAcquire(Personal).ShouldBe(SyncLockAttemptFactory.CreateBusy(background.PassId));
    }

    [Fact]
    public void when_a_pass_holds_the_account_then_a_background_lease_is_refused_as_busy()
    {
        using var pass = SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;

        SyncLock.TryAcquireBackground(Personal).ShouldBe(SyncLockAttemptFactory.CreateBusy(pass.PassId));
    }

    [Fact]
    public async Task when_the_lock_is_drained_then_a_background_lease_and_an_acquire_are_refused_as_draining()
    {
        using var drain = await SyncLock.DrainAsync(CancellationToken);

        (SyncLock.TryAcquireBackground(Personal), await SyncLock.AcquireAsync(Personal, CancellationToken)).ShouldBe((SyncLockAttemptFactory.CreateDraining(), SyncLockAttemptFactory.CreateDraining()));
    }

    [Fact]
    public async Task when_nothing_holds_the_account_then_acquire_takes_the_lease_at_once()
    {
        var acquiring = SyncLock.AcquireAsync(Personal, CancellationToken);

        var completedAtOnce = acquiring.IsCompleted;
        using var lease = (await acquiring).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        (completedAtOnce, lease.AccountId).ShouldBe((true, Personal));
    }

    [Fact]
    public async Task when_a_pass_holds_the_account_then_acquire_refuses_at_once_as_busy()
    {
        using var pass = SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;

        var acquiring = SyncLock.AcquireAsync(Personal, CancellationToken);

        (acquiring.IsCompleted, await acquiring).ShouldBe((true, SyncLockAttemptFactory.CreateBusy(pass.PassId)));
    }

    [Fact]
    public async Task when_a_background_lease_is_held_then_acquire_waits_for_its_release_and_takes_the_lease()
    {
        var background = Background(Personal);

        var acquiring = SyncLock.AcquireAsync(Personal, CancellationToken);
        var waitedForTheWrite = !acquiring.IsCompleted;
        background.Dispose();

        using var lease = (await acquiring).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        (waitedForTheWrite, lease.AccountId).ShouldBe((true, Personal));
    }

    [Fact]
    public async Task when_another_account_has_a_background_lease_then_acquire_does_not_wait()
    {
        using var background = Background(Work);

        var acquiring = SyncLock.AcquireAsync(Personal, CancellationToken);

        acquiring.IsCompleted.ShouldBeTrue();
        using var lease = (await acquiring).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
    }

    [Fact]
    public async Task when_a_caller_is_waiting_then_no_new_background_lease_starts_and_the_caller_gets_the_lease()
    {
        var background = Background(Personal);
        var acquiring = SyncLock.AcquireAsync(Personal, CancellationToken);

        background.Dispose();
        var reclassifyAgain = SyncLock.TryAcquireBackground(Personal);

        using var lease = (await acquiring).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        reclassifyAgain.ShouldBeOfType<SyncLockAttempt.Busy>();
    }

    [Fact]
    public async Task when_the_background_lease_is_never_released_then_acquire_gives_up_as_busy_after_the_wait_policy()
    {
        using var background = Background(Personal);
        var acquiring = SyncLock.AcquireAsync(Personal, CancellationToken);

        time.Advance(AccountLeasePolicy.WaitForRunningWork - TimeSpan.FromMilliseconds(1));
        var waitingJustBeforeTheLimit = !acquiring.IsCompleted;
        time.Advance(TimeSpan.FromMilliseconds(1));

        (waitingJustBeforeTheLimit, await acquiring).ShouldBe((true, SyncLockAttemptFactory.CreateBusy(background.PassId)));
    }

    [Fact]
    public async Task when_the_wait_is_cancelled_then_acquire_returns_the_refusal_and_background_work_may_start_again()
    {
        var background = Background(Personal);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var acquiring = SyncLock.AcquireAsync(Personal, cancellation.Token);

        await cancellation.CancelAsync();
        var attempt = await acquiring;
        background.Dispose();

        attempt.ShouldBe(SyncLockAttemptFactory.CreateBusy(background.PassId));
        using var again = Background(Personal);
    }

    [Fact]
    public async Task when_twenty_callers_wait_on_one_background_lease_then_exactly_one_gets_the_lease()
    {
        var background = Background(Personal);
        var callers = Enumerable.Range(0, 20).Select(_ => SyncLock.AcquireAsync(Personal, CancellationToken)).ToList();
        var allWaiting = callers.All(caller => !caller.IsCompleted);

        background.Dispose();
        var attempts = await Task.WhenAll(callers);

        var acquired = attempts.OfType<SyncLockAttempt.Acquired>().ToList();
        allWaiting.ShouldBeTrue();
        acquired.Count.ShouldBe(1);
        attempts.Count(attempt => attempt is SyncLockAttempt.Busy).ShouldBe(19);
        acquired[0].Lease.Dispose();
    }

    public void Dispose() => provider.Dispose();
}
