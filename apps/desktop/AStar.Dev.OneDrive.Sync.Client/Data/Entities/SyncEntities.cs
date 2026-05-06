using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents a synchronization conflict for a specific file between the local system and OneDrive. This entity captures details about the conflicting item, including its paths, modification times, sizes, and the current state of the conflict. It also tracks when the conflict was detected and resolved, as well as the resolution policy applied if any.
/// </summary>
public sealed class SyncConflictEntity
{
    /// <summary>
    /// The unique identifier for the synchronization conflict. This is used to track and manage individual conflicts within the system, allowing for resolution and historical reference.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the OneDrive account associated with this conflict. This links the conflict to a specific user account, enabling account-specific conflict management and resolution.
    /// </summary>
    public AccountId AccountId { get; set; }

    /// <summary>
    /// The identifier of the OneDrive folder containing the conflicting item. This is used to locate the item within the OneDrive structure and to provide context for the conflict, such as its location and relationship to other items in the drive.
    /// </summary>
    public OneDriveFolderId FolderId { get; set; }

    /// <summary>
    /// The identifier of the conflicting item in OneDrive. This is used to track the specific item that is in conflict, allowing for targeted resolution and management of the conflict based on the item's unique identifier within OneDrive.
    /// </summary>
    public OneDriveItemId RemoteItemId { get; set; }

    /// <summary>
    /// The relative path of the conflicting item within the OneDrive folder structure. This is used for display purposes and to provide context for the conflict, allowing users to understand where the item is located within their OneDrive and to make informed decisions about how to resolve the conflict. The RelativePath can be constructed based on the folder structure and the item's name, providing a user-friendly representation of the item's location in OneDrive while still maintaining the necessary information for synchronization tasks.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// The local path of the conflicting item on the user's file system. This is used for display purposes and to provide context for the conflict, allowing users to understand where the item is located on their local system and to make informed decisions about how to resolve the conflict. The LocalPath is essential for managing the synchronization process, as it allows the sync client to determine where to read or write files during sync operations and to handle conflicts when changes occur both locally and remotely. By providing a clear representation of the item's location on the local file system, users can more easily navigate to the item and take appropriate actions to resolve the conflict.
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the last modification of the item on the local file system. This is used to track changes to the item locally and to determine whether synchronization operations are needed based on the modification time. By comparing the LocalModified timestamp with the RemoteModified timestamp, the sync client can identify when an item has been changed locally and needs to be updated remotely, or when a conflict has occurred due to concurrent modifications both locally and remotely. This property is essential for maintaining data consistency and ensuring that the most up-to-date version of the item is available in both locations after synchronization.
    /// </summary>
    public DateTimeOffset LocalModified { get; set; }

    /// <summary>
    /// The timestamp of the last modification of the item in OneDrive, provided by the Microsoft Graph API. This is used to track changes to the item in OneDrive and to determine whether synchronization operations are needed based on the modification time. By comparing the RemoteModified timestamp with the LocalModified timestamp, the sync client can identify when an item has been changed remotely and needs to be updated locally, or when a conflict has occurred due to concurrent modifications both locally and remotely. This property is essential for maintaining data consistency and ensuring that the most up-to-date version of the item is available in both locations after synchronization.
    /// </summary>
    public DateTimeOffset RemoteModified { get; set; }

    /// <summary>
    /// The size of the item on the local file system in bytes. This is used to track the size of the item locally and to determine whether synchronization operations are needed based on the size. By comparing the LocalSize with the RemoteSize, the sync client can identify when an item has been changed locally and needs to be updated remotely, or when a conflict has occurred due to concurrent modifications both locally and remotely. This property is essential for maintaining data consistency and ensuring that the most up-to-date version of the item is available in both locations after synchronization.
    /// </summary>
    public long LocalSize { get; set; }

    /// <summary>
    /// The size of the item in OneDrive in bytes, provided by the Microsoft Graph API. This is used to track the size of the item in OneDrive and to determine whether synchronization operations are needed based on the size. By comparing the RemoteSize with the LocalSize, the sync client can identify when an item has been changed remotely and needs to be updated locally, or when a conflict has occurred due to concurrent modifications both locally and remotely. This property is essential for maintaining data consistency and ensuring that the most up-to-date version of the item is available in both locations after synchronization.
    /// </summary>
    public long RemoteSize { get; set; }

    /// <summary>
    /// The current state of the conflict, indicating whether it is pending resolution, has been resolved, or is in another state. This property is used to track the progress of conflict resolution and to determine what actions are needed to resolve the conflict. By maintaining the state of the conflict, the sync client can provide appropriate prompts and options to the user for resolving the conflict, as well as track the history of conflicts and their resolutions for future reference.
    /// </summary>
    public ConflictState State { get; set; } = ConflictState.Pending;

    /// <summary>
    /// The resolution policy applied to the conflict, if any. This property indicates how the conflict was resolved, such as whether the local version was kept, the remote version was kept, or a manual merge was performed. By tracking the resolution policy, the sync client can provide insights into how conflicts are being resolved and can help users understand the outcomes of their conflict resolution actions. This information can also be used for auditing purposes and for improving the conflict resolution process in future iterations of the sync client application.
    /// </summary>
    public ConflictPolicy? Resolution { get; set; }

    /// <summary>
    /// The timestamp when the conflict was detected. This is used to track when the conflict was first identified, allowing for historical reference and analysis of conflict occurrences over time. By maintaining the DetectedAt timestamp, the sync client can provide insights into the frequency and timing of conflicts, which can be valuable for troubleshooting and improving the synchronization process. Additionally, this information can help users understand how long a conflict has been pending resolution and can prompt them to take action if necessary.
    /// </summary>
    public DateTimeOffset DetectedAt { get; set; }

    /// <summary>
    /// The timestamp when the conflict was resolved, if applicable. This is used to track when the conflict was successfully resolved, allowing for historical reference and analysis of conflict resolution over time. By maintaining the ResolvedAt timestamp, the sync client can provide insights into how long conflicts typically take to resolve and can help users understand the outcomes of their conflict resolution actions. Additionally, this information can be used for auditing purposes and for improving the conflict resolution process in future iterations of the sync client application.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// Navigation property to the associated AccountEntity, allowing for access to the account's profile information, sync configuration, and other related data. This relationship is established through the AccountId foreign key, enabling the sync client to easily retrieve and manage the synchronization conflict in the context of the corresponding account. The navigation property is marked as nullable to indicate that there may be cases where the account information is not available or has been deleted, allowing for graceful handling of such scenarios within the application.
    /// </summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
