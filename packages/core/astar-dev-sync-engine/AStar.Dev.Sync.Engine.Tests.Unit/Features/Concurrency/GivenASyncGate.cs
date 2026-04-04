using AStar.Dev.Sync.Engine.Features.Concurrency;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.Concurrency;

public sealed class GivenASyncGate
{
    [Fact]
    public void when_two_syncs_start_for_the_same_account_then_the_second_is_rejected()
    {
        using var gate = new SyncGate();

        var firstAcquired = gate.TryBeginSync("account-a");
        var secondAcquired = gate.TryBeginSync("account-a");

        firstAcquired.ShouldBeTrue();
        secondAcquired.ShouldBeFalse();
    }

    [Fact]
    public void when_two_syncs_start_for_different_accounts_then_both_are_permitted()
    {
        using var gate = new SyncGate();

        var firstAcquired = gate.TryBeginSync("account-a");
        var secondAcquired = gate.TryBeginSync("account-b");

        firstAcquired.ShouldBeTrue();
        secondAcquired.ShouldBeTrue();
    }

    [Fact]
    public void when_no_sync_is_running_then_is_any_account_syncing_returns_false()
    {
        using var gate = new SyncGate();

        gate.IsAnyAccountSyncing().ShouldBeFalse();
    }

    [Fact]
    public void when_a_sync_is_running_then_is_any_account_syncing_returns_true()
    {
        using var gate = new SyncGate();
        gate.TryBeginSync("account-a");

        gate.IsAnyAccountSyncing().ShouldBeTrue();
    }

    [Fact]
    public void when_sync_ends_then_same_account_can_begin_again()
    {
        using var gate = new SyncGate();
        gate.TryBeginSync("account-a");
        gate.EndSync("account-a");

        var reacquired = gate.TryBeginSync("account-a");

        reacquired.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(11)]
    [InlineData(20)]
    public void when_max_concurrent_transfers_is_outside_range_then_it_is_clamped(int requested)
    {
        using var gate = new SyncGate(requested);
        var slots = gate.GetTransferSlots("account-a");

        slots.CurrentCount.ShouldBeInRange(1, 10);
    }

    [Fact]
    public void when_max_concurrent_transfers_is_within_range_then_slot_count_matches()
    {
        using var gate = new SyncGate(3);
        var slots = gate.GetTransferSlots("account-a");

        slots.CurrentCount.ShouldBe(3);
    }
}
