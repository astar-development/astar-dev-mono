using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>What startup recovery did for an account whose last run was interrupted (#1610).</summary>
public abstract record RecoveryOutcome
{
    private RecoveryOutcome()
    {
    }

    /// <summary>The interrupted run was marked and a normal pass with the recovery trigger ran.</summary>
    public sealed record Resumed : RecoveryOutcome
    {
        internal Resumed(AccountId accountId)
        {
            AccountId = accountId;
        }

        /// <summary>The account.</summary>
        public AccountId AccountId { get; }
    }

    /// <summary>The interrupted run was marked, but the recovery pass could not run until the user fixes <see cref="Blocker"/> (#1612).</summary>
    public sealed record NotPossible : RecoveryOutcome
    {
        internal NotPossible(AccountId accountId, RecoveryBlocker blocker)
        {
            AccountId = accountId;
            Blocker = blocker;
        }

        /// <summary>The account.</summary>
        public AccountId AccountId { get; }

        /// <summary>What has to be fixed first.</summary>
        public RecoveryBlocker Blocker { get; }
    }

    /// <summary>The interrupted run was marked, but the recovery pass did not finish.</summary>
    public sealed record NotResumed : RecoveryOutcome
    {
        internal NotResumed(AccountId accountId, SyncPassFailure reason)
        {
            AccountId = accountId;
            Reason = reason;
        }

        /// <summary>The account.</summary>
        public AccountId AccountId { get; }

        /// <summary>Why the recovery pass did not finish.</summary>
        public SyncPassFailure Reason { get; }
    }

    /// <summary>The interrupted run was marked, but no recovery pass was started because the account is paused or the paused accounts could not be read (#1616).</summary>
    public sealed record Held : RecoveryOutcome
    {
        internal Held(AccountId accountId)
        {
            AccountId = accountId;
        }

        /// <summary>The account.</summary>
        public AccountId AccountId { get; }
    }
}
