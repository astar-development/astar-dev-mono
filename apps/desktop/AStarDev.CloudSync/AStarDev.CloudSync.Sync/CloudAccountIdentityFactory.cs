using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="CloudAccountIdentity"/> values.</summary>
public static class CloudAccountIdentityFactory
{
    /// <summary>Creates an identity. Missing text is normalised to empty strings.</summary>
    public static CloudAccountIdentity Create(AccountId id, string displayName, string email, CloudProvider provider) => new(id, displayName ?? string.Empty, email ?? string.Empty, provider);
}
