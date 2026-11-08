namespace AStarDev.CloudSync.Accounts;

/// <summary>The result of one attempt to add an account.</summary>
public abstract record AccountAddOutcome
{
    private AccountAddOutcome()
    {
    }

    /// <summary>A new account was added.</summary>
    public sealed record Added(AccountId AccountId) : AccountAddOutcome;

    /// <summary>The account was already added; it was signed in again and its settings were kept.</summary>
    public sealed record ReAuthenticated(AccountId AccountId) : AccountAddOutcome;

    /// <summary>The user cancelled the attempt.</summary>
    public sealed record Cancelled : AccountAddOutcome;

    /// <summary>The attempt failed.</summary>
    public sealed record Failed(AccountAddFailure Failure) : AccountAddOutcome;
}
