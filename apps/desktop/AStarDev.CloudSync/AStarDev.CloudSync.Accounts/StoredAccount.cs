namespace AStarDev.CloudSync.Accounts;

/// <summary>An account as CloudSync has saved it.</summary>
/// <param name="Id">CloudSync's id for the account.</param>
/// <param name="Registration">The account details from its latest sign-in.</param>
public sealed record StoredAccount(AccountId Id, AccountRegistration Registration);
