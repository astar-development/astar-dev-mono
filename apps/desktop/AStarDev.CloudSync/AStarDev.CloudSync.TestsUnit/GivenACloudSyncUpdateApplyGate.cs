using AStar.Dev.Velopack.Publishing;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.Updates;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenACloudSyncUpdateApplyGate : IDisposable
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly ServiceProvider provider = new ServiceCollection().AddLogging().AddCloudSync().BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    private CloudSyncUpdateApplyGate Create() => new(SyncLock);

    public void Dispose() => provider.Dispose();

    [Fact]
    public async Task when_nothing_is_syncing_then_the_gate_lets_the_update_go_at_once_and_new_passes_are_refused_while_the_permit_is_held()
    {
        using var permit = await Create().WaitUntilSafeAsync(CancellationToken);

        SyncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Draining>();
    }

    [Fact]
    public async Task when_a_pass_is_running_then_the_gate_waits_for_it_and_refuses_new_passes_meanwhile()
    {
        var running = SyncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        var waiting = Create().WaitUntilSafeAsync(CancellationToken);
        await Task.Delay(50, CancellationToken);
        var otherAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        var whileWaiting = (waiting.IsCompleted, SyncLock.TryAcquire(otherAccount).GetType());
        running.Dispose();
        using var permit = await waiting.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        whileWaiting.ShouldBe((false, typeof(SyncLockAttempt.Draining)));
    }

    [Fact]
    public async Task when_the_permit_is_disposed_then_syncing_can_start_again()
    {
        var permit = await Create().WaitUntilSafeAsync(CancellationToken);

        permit.Dispose();

        using var lease = SyncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        lease.AccountId.ShouldBe(Account);
    }

    [Fact]
    public async Task when_waiting_is_cancelled_then_it_stops_and_syncing_is_allowed_again()
    {
        using var running = SyncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        using var cancel = new CancellationTokenSource();
        var waiting = Create().WaitUntilSafeAsync(cancel.Token);
        await Task.Delay(50, CancellationToken);

        await cancel.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => waiting);
        running.Dispose();
        SyncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease.Dispose();
    }

    [Fact]
    public void when_the_gate_is_created_then_it_is_an_update_apply_gate()
    {
        Create().ShouldBeAssignableTo<IUpdateApplyGate>();
    }
}
