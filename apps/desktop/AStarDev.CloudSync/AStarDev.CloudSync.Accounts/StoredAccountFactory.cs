
namespace AStarDev.CloudSync.Accounts;

/// <summary>Creates <see cref="StoredAccount"/> values.</summary>
public static class StoredAccountFactory
{
    /// <summary>Creates a stored account.</summary>
    /// <param name="id">CloudSync's id for the account.</param>
    /// <param name="registration">The account details from its latest sign-in.</param>
    public static StoredAccount Create(AccountId id, AccountRegistration registration) => new(id, registration);
}
