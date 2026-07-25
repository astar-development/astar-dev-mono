using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents a OneDrive account connected to the sync client, including profile info, sync state, and configuration.
/// </summary>
public sealed class AccountEntity
{
    /// <summary>Unique identifier for the account.</summary>
    public AccountId Id { get; set; } = new("Unknown");

    /// <summary>User profile information associated with the account.</summary>
    public AccountProfile Profile { get; set; } = AccountProfileFactory.Empty;

    /// <summary>Index of the accent colour chosen by the user for this account.</summary>
    public int AccentIndex { get; set; }

    /// <summary>Indicates whether the account is currently active and should be included in sync operations.</summary>
    public bool IsActive { get; set; }

    /// <summary>Timestamp of the last successful sync operation for this account, or None if never synced.</summary>
    public Option<DateTimeOffset> LastSyncedAt { get; set; } = Option.None<DateTimeOffset>();

    /// <summary>Storage quota information for the account.</summary>
    public StorageQuota Quota { get; set; } = StorageQuotaFactory.Unknown;

    /// <summary>User-configurable sync settings for the account.</summary>
    public AccountSyncConfig SyncConfig { get; set; } = AccountSyncConfigFactory.Default;
}
