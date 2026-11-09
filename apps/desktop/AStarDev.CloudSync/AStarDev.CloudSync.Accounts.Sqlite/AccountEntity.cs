namespace AStarDev.CloudSync.Accounts.Sqlite;

/// <summary>The database row for a saved account.</summary>
internal sealed class AccountEntity
{
    /// <summary>CloudSync's id for the account.</summary>
    public Guid Id { get; set; }

    /// <summary>The provider's key, for example <c>OneDrive</c>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>The provider's identifier for the account, compared ignoring case.</summary>
    public string ProviderAccountKey { get; set; } = string.Empty;

    /// <summary>The account's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The account's sign-in email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The provider's identifier for the account's drive.</summary>
    public string DriveId { get; set; } = string.Empty;

    /// <summary>Cloud storage used, in bytes.</summary>
    public long QuotaUsedBytes { get; set; }

    /// <summary>Cloud storage available, in bytes.</summary>
    public long QuotaTotalBytes { get; set; }
}
