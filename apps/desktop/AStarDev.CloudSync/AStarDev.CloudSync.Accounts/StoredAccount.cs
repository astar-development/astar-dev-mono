namespace AStarDev.CloudSync.Accounts;

/// <summary>An account as CloudSync has saved it.</summary>
public sealed record StoredAccount
{
    internal StoredAccount(AccountId id, AccountRegistration registration)
    {
        Id = id;
        Registration = registration;
    }

    /// <summary>CloudSync's id for the account.</summary>
    public AccountId Id { get; }

    /// <summary>The account details from its latest sign-in.</summary>
    public AccountRegistration Registration { get; }
}
