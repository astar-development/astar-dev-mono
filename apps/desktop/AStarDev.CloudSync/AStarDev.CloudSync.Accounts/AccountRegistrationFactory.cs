
namespace AStarDev.CloudSync.Accounts;

/// <summary>Creates <see cref="AccountRegistration"/> values.</summary>
public static class AccountRegistrationFactory
{
    /// <summary>Creates an account registration. A missing display name or email becomes an empty string; both are trimmed.</summary>
    public static AccountRegistration Create(ProviderAccountKey key, CloudProvider provider, string? displayName, string? email, DriveId driveId, StorageUsage storage) => new(key, provider, displayName?.Trim() ?? string.Empty, email?.Trim() ?? string.Empty, driveId, storage);
}
