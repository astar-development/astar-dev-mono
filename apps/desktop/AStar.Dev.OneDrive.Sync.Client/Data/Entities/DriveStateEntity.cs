using System.ComponentModel.DataAnnotations.Schema;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents the synchronization state of a OneDrive account, including the latest delta link for tracking changes and the timestamp of the last sync operation. This entity is used to manage and persist the state of each account's synchronization process within the client application.
/// </summary>
public sealed class DriveStateEntity
{
    /// <summary>
    /// The unique identifier for the drive state record. This is typically used as the primary key in the database and is auto-generated. It allows for efficient retrieval and management of the drive state information associated with a specific OneDrive account.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The identifier of the OneDrive account associated with this drive state. This is a foreign key that links to the AccountEntity, allowing for the association of the drive state with the corresponding account profile and synchronization configuration. It is essential for tracking the synchronization status and changes for each individual account within the sync client.
    /// </summary>
    public AccountId AccountId { get; set; }

    /// <summary>
    /// The delta link provided by Microsoft Graph API for OneDrive, which is used to track changes in the drive since the last synchronization. This link allows the sync client to efficiently query for changes without having to retrieve the entire drive contents, enabling faster sync operations and reduced bandwidth usage. The delta link is updated after each successful sync operation to reflect the latest state of the drive.
    /// </summary>
    public string? DeltaLink { get; set; }

    /// <summary>
    /// The timestamp of the last successful synchronization operation for the associated OneDrive account. This information is crucial for determining when the next sync should occur and for displaying sync status in the user interface. It can also be used for debugging purposes to identify potential issues with synchronization and to ensure that the sync client is operating as expected. If this value is null, it indicates that a sync has not yet been performed for this account.
    /// </summary>
    public DateTimeOffset? LastSyncStartedAt { get; set; }

    /// <summary>
    /// Navigation property to the associated AccountEntity, allowing for access to the account's profile information, sync configuration, and other related data. This relationship is established through the AccountId foreign key, enabling the sync client to easily retrieve and manage the drive state in the context of the corresponding account. The navigation property is marked as nullable to indicate that there may be cases where the account information is not available or has been deleted, allowing for graceful handling of such scenarios within the application.
    /// </summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
