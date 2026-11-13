using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>A sync run started for an account.</summary>
/// <param name="accountId">The account.</param>
/// <param name="trigger">What started the run.</param>
/// <param name="startedAt">When the run started.</param>
public sealed class SyncPassStartedEventArgs(AccountId accountId, SyncTrigger trigger, DateTimeOffset startedAt) : EventArgs
{
    /// <summary>The account.</summary>
    public AccountId AccountId { get; } = accountId;

    /// <summary>What started the run.</summary>
    public SyncTrigger Trigger { get; } = trigger;

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; } = startedAt;
}
