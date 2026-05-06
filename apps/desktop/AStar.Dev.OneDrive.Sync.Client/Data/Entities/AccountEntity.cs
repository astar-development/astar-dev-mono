namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents a OneDrive account connected to the sync client, including profile info, sync state, and configuration.
/// </summary>
public sealed class AccountEntity
{
    /// <summary>
    /// Unique identifier for the account, typically the Microsoft Graph user ID or a generated GUID for non-Microsoft accounts.
    /// </summary>
    public AccountId Id { get; set; } = new("Unknown");

    /// <summary>
    /// User profile information associated with the account, such as display name and email. This may be populated from Microsoft Graph or other identity providers depending on the account type.
    /// </summary>
    public AccountProfile Profile { get; set; } = AccountProfileFactory.Empty;

    /// <summary>
    /// Index of the accent color chosen by the user for this account, used for UI theming. This is typically an integer corresponding to a predefined set of colors in the client application.
    /// </summary>
    public int AccentIndex { get; set; }

    /// <summary>
    /// Indicates whether the account is currently active and should be included in sync operations. Inactive accounts may be those that have been disabled by the user or have encountered authentication issues. This flag can be used to filter out accounts during sync and UI display without permanently deleting the account information.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp of the last successful sync operation for this account. This can be used to determine when the account was last synced and to trigger sync operations for accounts that have not been synced recently. It may also be useful for displaying sync status in the UI or for debugging sync issues.
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    /// <summary>
    /// Storage quota information for the account, including total storage, used storage, and remaining storage. This information can be retrieved from Microsoft Graph for OneDrive accounts or from other APIs for non-Microsoft accounts. It is useful for displaying storage usage in the UI and for making decisions about syncing large files when storage limits are approaching.
    /// </summary>
    public StorageQuota Quota { get; set; } = StorageQuotaFactory.Unknown;

    /// <summary>
    /// User-configurable sync settings for the account, such as which folders to include or exclude from syncing, sync frequency, and conflict resolution policies. This configuration can be modified by the user through the client UI and is used to control the behavior of the sync engine for this account. It may also include settings specific to certain account types, such as OneDrive for Business or SharePoint document libraries.
    /// </summary>
    public AccountSyncConfig SyncConfig { get; set; } = AccountSyncConfigFactory.Default;
}
