namespace AStarDev.CloudSync.Accounts;

/// <summary>What a successful sign-in tells CloudSync about an account, ready to be saved.</summary>
/// <param name="Key">The provider's own identifier for the account.</param>
/// <param name="Provider">The provider the account belongs to.</param>
/// <param name="DisplayName">The account's display name.</param>
/// <param name="Email">The account's sign-in email.</param>
/// <param name="DriveId">The provider's identifier for the account's drive.</param>
/// <param name="Storage">Cloud storage used and available.</param>
public sealed record AccountRegistration(ProviderAccountKey Key, CloudProvider Provider, string DisplayName, string Email, DriveId DriveId, StorageUsage Storage);
