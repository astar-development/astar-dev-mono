using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents an item that has been synchronized between the local file system and OneDrive, including metadata such as remote and local paths, modification timestamps, and synchronization tags. This entity is used to track the state of each synchronized item within the sync client application, allowing for efficient synchronization operations and conflict resolution based on the stored information.
/// </summary>
public sealed class SyncedItemEntity
{
    /// <summary>
    /// The unique identifier for the synchronized item within the local database. This is typically an auto-incrementing integer that serves as the primary key for the SyncedItemEntity table, allowing for efficient querying and management of synchronized items in relation to their corresponding OneDrive items and accounts.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The identifier of the OneDrive account associated with this synchronized item. This is a foreign key that links to the AccountEntity, allowing for the association of the synchronized item with the corresponding account profile and synchronization configuration. It is essential for tracking which items belong to which accounts, especially in scenarios where multiple OneDrive accounts are being synchronized within the same client application.
    /// </summary>
    public AccountId AccountId { get; set; }

    /// <summary>
    /// The unique identifier of the item in OneDrive, provided by the Microsoft Graph API. This identifier is used to track the corresponding item in OneDrive and to perform synchronization operations such as updates, deletions, and conflict resolution. It is crucial for maintaining the link between the local representation of the item and its remote counterpart in OneDrive, enabling the sync client to manage changes effectively and ensure data consistency across both locations.
    /// </summary>
    public OneDriveItemId RemoteItemId { get; set; }

    /// <summary>
    /// The identifier of the parent folder in OneDrive, provided by the Microsoft Graph API. This is used to track the hierarchical structure of items in OneDrive and to manage synchronization operations that involve moving items between folders or creating new items within specific folders. By storing the RemoteParentId, the sync client can maintain an accurate representation of the folder structure in OneDrive and ensure that changes to item locations are properly synchronized between the local file system and OneDrive.
    /// </summary>
    public string RemoteParentId { get; set; } = string.Empty;

    /// <summary>
    /// The remote path of the item in OneDrive, which represents the full path to the item within the OneDrive folder structure. This is used for display purposes in the user interface and for performing synchronization operations that require knowledge of the item's location in OneDrive. The RemotePath can be constructed based on the RemoteParentId and the item's name, allowing the sync client to provide a user-friendly representation of the item's location in OneDrive while still maintaining the necessary information for synchronization tasks.
    /// </summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>
    /// The local path of the item on the user's file system, which represents the full path to the item within the local folder structure. This is used for display purposes in the user interface and for performing synchronization operations that require knowledge of the item's location on the local file system. The LocalPath is essential for managing the synchronization process, as it allows the sync client to determine where to read or write files during sync operations and to handle conflicts when changes occur both locally and remotely.
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the synchronized item is a folder or a file. This information is crucial for determining how to handle the item during synchronization operations, as folders and files have different behaviors and requirements. For example, when synchronizing a folder, the sync client needs to ensure that all child items are also synchronized, while for files, the sync client can focus on the individual item. Additionally, this property can be used for display purposes in the user interface, allowing users to easily distinguish between folders and files in their synchronized items list.
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>
    /// The timestamp of the last modification of the item in OneDrive, provided by the Microsoft Graph API. This is used to track changes to the item in OneDrive and to determine whether synchronization operations are needed based on the modification time. By comparing the RemoteModifiedAt timestamp with the local modification time, the sync client can identify when an item has been changed remotely and needs to be updated locally, or when a conflict has occurred due to concurrent modifications both locally and remotely. This property is essential for maintaining data consistency and ensuring that the most up-to-date version of the item is available in both locations after synchronization.
    /// </summary>
    public DateTimeOffset RemoteModifiedAt { get; set; }

    /// <summary>
    /// The synchronization tags associated with the item, which are used to track the state of the item during synchronization operations. These tags can include information such as the version of the item, the last sync operation performed, and any conflicts that may have occurred. By storing synchronization tags, the sync client can manage the synchronization process more effectively, allowing for efficient conflict resolution and ensuring that changes are properly tracked and applied during sync operations. The Tags property can be updated after each synchronization operation to reflect the latest state of the item and to provide necessary information for future sync operations.
    /// </summary>
    public VersionInfo Tags { get; set; } = VersionInfoFactory.Create(Option.None<string>(), Option.None<string>());

    /// <summary>
    /// The size of the file in bytes at the time it was last synchronized. Null for folders or when size was not available at sync time.
    /// </summary>
    public long? SizeInBytes { get; set; }

    /// <summary>
    /// Foreign key to the canonical <see cref="FileDetailEntity"/> representing the physical file on disk. Null for folders and for items registered before file-level classification unification. Classifications hang off the file detail, so linking here lets every application share one set of classifications per file.
    /// </summary>
    public FileId? FileDetailId { get; set; }

    /// <summary>
    /// Navigation property to the canonical file detail for this synced item, when linked.
    /// </summary>
    public FileDetailEntity? FileDetail { get; set; }

    /// <summary>
    /// Navigation property to the associated AccountEntity, allowing for access to the account's profile information, sync configuration, and other related data. This relationship is established through the AccountId foreign key, enabling the sync client to easily retrieve and manage the synchronized item in the context of the corresponding account. The navigation property is marked as nullable to indicate that there may be cases where the account information is not available or has been deleted, allowing for graceful handling of such scenarios within the application.
    /// </summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
