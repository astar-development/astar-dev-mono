using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="AccountAddOutcome"/> cases.</summary>
public static class AccountAddOutcomeFactory
{
    /// <summary>Creates the outcome for a newly added account.</summary>
    /// <param name="accountId">The new account's id.</param>
    public static AccountAddOutcome CreateAdded(AccountId accountId) => throw new NotImplementedException();

    /// <summary>Creates the outcome for an account that was already added and was signed in again.</summary>
    /// <param name="accountId">The existing account's id.</param>
    public static AccountAddOutcome CreateReAuthenticated(AccountId accountId) => throw new NotImplementedException();

    /// <summary>Creates the outcome for an attempt the user cancelled.</summary>
    public static AccountAddOutcome CreateCancelled() => throw new NotImplementedException();

    /// <summary>Creates the outcome for a failed attempt.</summary>
    /// <param name="failure">Why the attempt failed.</param>
    public static AccountAddOutcome CreateFailed(AccountAddFailure failure) => throw new NotImplementedException();
}
