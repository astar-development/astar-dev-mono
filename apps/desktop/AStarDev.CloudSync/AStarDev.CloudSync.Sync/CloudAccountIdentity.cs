using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Who an account is: its id, display name, sign-in email and provider.</summary>
/// <param name="Id">The account id.</param>
/// <param name="DisplayName">The account's display name.</param>
/// <param name="Email">The account's sign-in email.</param>
/// <param name="Provider">The provider the account belongs to.</param>
public sealed record CloudAccountIdentity(AccountId Id, string DisplayName, string Email, CloudProvider Provider);
