using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>A journaled run that has no end: the app stopped while it was running (#1610).</summary>
public sealed record InterruptedSyncRun
{
    internal InterruptedSyncRun(SyncRunId id, AccountId accountId)
    {
        Id = id;
        AccountId = accountId;
    }

    /// <summary>The run.</summary>
    public SyncRunId Id { get; }

    /// <summary>The account the run was for.</summary>
    public AccountId AccountId { get; }
}
