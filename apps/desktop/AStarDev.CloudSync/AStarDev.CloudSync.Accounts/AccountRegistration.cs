namespace AStarDev.CloudSync.Accounts;

/// <summary>What a successful sign-in tells CloudSync about an account, ready to be saved.</summary>
public sealed record AccountRegistration
{
    internal AccountRegistration(ProviderAccountKey key, CloudProvider provider, string displayName, string email, DriveId driveId, StorageUsage storage)
    {
        Key = key;
        Provider = provider;
        DisplayName = displayName;
        Email = email;
        DriveId = driveId;
        Storage = storage;
    }

    /// <summary>The provider's own identifier for the account.</summary>
    public ProviderAccountKey Key { get; }

    /// <summary>The provider the account belongs to.</summary>
    public CloudProvider Provider { get; }

    /// <summary>The account's display name.</summary>
    public string DisplayName { get; }

    /// <summary>The account's sign-in email.</summary>
    public string Email { get; }

    /// <summary>The provider's identifier for the account's drive.</summary>
    public DriveId DriveId { get; }

    /// <summary>Cloud storage used and available.</summary>
    public StorageUsage Storage { get; }
}
