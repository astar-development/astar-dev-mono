using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="AccountRegistration"/> values.</summary>
public static class AccountRegistrationFactory
{
    /// <summary>Creates an account registration. A missing display name or email becomes an empty string; both are trimmed.</summary>
    public static AccountRegistration Create(ProviderAccountKey key, CloudProvider provider, string? displayName, string? email, DriveId driveId, StorageUsage storage) => throw new NotImplementedException();
}
