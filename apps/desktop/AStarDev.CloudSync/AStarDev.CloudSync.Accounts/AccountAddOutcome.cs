namespace AStarDev.CloudSync.Accounts;

/// <summary>The result of one attempt to add an account.</summary>
public abstract record AccountAddOutcome
{
    private AccountAddOutcome()
    {
    }

    /// <summary>A new account was added.</summary>
    public sealed record Added : AccountAddOutcome
    {
        internal Added(AccountId accountId)
        {
            AccountId = accountId;
        }

        /// <summary>The id of the new account.</summary>
        public AccountId AccountId { get; }
    }

    /// <summary>The account was already added; it was signed in again and its settings were kept.</summary>
    public sealed record ReAuthenticated : AccountAddOutcome
    {
        internal ReAuthenticated(AccountId accountId)
        {
            AccountId = accountId;
        }

        /// <summary>The id of the account that was signed in again.</summary>
        public AccountId AccountId { get; }
    }

    /// <summary>The user cancelled the attempt.</summary>
    public sealed record Cancelled : AccountAddOutcome
    {
        internal Cancelled()
        {
        }
    }

    /// <summary>The attempt failed.</summary>
    public sealed record Failed : AccountAddOutcome
    {
        internal Failed(AccountAddFailure failure)
        {
            Failure = failure;
        }

        /// <summary>Why the attempt failed.</summary>
        public AccountAddFailure Failure { get; }
    }
}
