using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A sync run ended for an account.</summary>
/// <param name="accountId">The account.</param>
/// <param name="trigger">What started the run.</param>
/// <param name="outcome">What the run did, or why it did not finish.</param>
public sealed class SyncPassFinishedEventArgs(AccountId accountId, SyncTrigger trigger, Result<SyncPassSummary, SyncPassFailure> outcome) : EventArgs
{
    /// <summary>The account.</summary>
    public AccountId AccountId { get; } = accountId;

    /// <summary>What started the run.</summary>
    public SyncTrigger Trigger { get; } = trigger;

    /// <summary>What the run did, or why it did not finish.</summary>
    public Result<SyncPassSummary, SyncPassFailure> Outcome { get; } = outcome;
}
