using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents a synchronization job for a specific OneDrive item, including details about the account, item identifiers, paths, synchronization direction, state, and timestamps. This entity is used to track the progress and status of synchronization operations within the sync client, allowing for efficient management of sync tasks and handling of any errors that may occur during the process.
/// </summary>
public sealed class SyncJobEntity
{
    /// <summary>
    /// The unique identifier for the synchronization job, represented as a GUID. This serves as the primary key for the SyncJobEntity and allows for efficient tracking and management of individual sync jobs within the database. The Id is generated when a new sync job is created and remains constant throughout the lifecycle of the job, enabling reliable referencing and retrieval of sync job details as needed.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the OneDrive account associated with this synchronization job. This is a foreign key that links to the AccountEntity, allowing for the association of the sync job with the corresponding account profile and synchronization configuration. It is essential for tracking the synchronization tasks and their outcomes for each individual account within the sync client, enabling account-specific management of sync operations and error handling.
    /// </summary>
    public AccountId AccountId { get; set; }

    /// <summary>
    /// The identifier of the OneDrive folder containing the item being synchronized. This is used to locate the item within the OneDrive structure and to provide context for the synchronization job, such as its location and relationship to other items in the drive. The FolderId is crucial for managing synchronization operations that involve moving items between folders or creating new items within specific folders, allowing the sync client to maintain an accurate representation of the folder structure in OneDrive and ensure that changes to item locations are properly synchronized between the local file system and OneDrive.
    /// </summary>
    public OneDriveFolderId FolderId { get; set; }

    /// <summary>
    /// The identifier of the item being synchronized in OneDrive, provided by the Microsoft Graph API. This is used to track the specific item that is being synchronized, allowing for targeted synchronization operations and management of the sync job based on the item's unique identifier within OneDrive. The RemoteItemId is essential for maintaining the link between the local representation of the item and its remote counterpart in OneDrive, enabling the sync client to manage changes effectively and ensure data consistency across both locations during synchronization.
    /// </summary>
    public OneDriveItemId RemoteItemId { get; set; }

    /// <summary>
    /// The relative path of the item being synchronized within the OneDrive folder structure. This is used for display purposes and to provide context for the synchronization job, allowing users to understand where the item is located within their OneDrive and to make informed decisions about synchronization operations. The RelativePath can be constructed based on the folder structure and the item's name, providing a user-friendly representation of the item's location in OneDrive while still maintaining the necessary information for synchronization tasks.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// The local path of the item being synchronized on the user's file system. This is used for display purposes and to provide context for the synchronization job, allowing users to understand where the item is located on their local system and to make informed decisions about synchronization operations. The LocalPath is essential for managing the synchronization process, as it allows the sync client to determine where to read or write files during sync operations and to handle conflicts when changes occur both locally and remotely. By providing a clear representation of the item's location on the local file system, users can more easily navigate to the item and take appropriate actions during synchronization.
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// The direction of the synchronization operation, indicating whether the sync job is an upload (from local to OneDrive) or a download (from OneDrive to local). This information is crucial for determining how to handle the synchronization process, as uploads and downloads have different behaviors and requirements. For example, when performing an upload, the sync client needs to ensure that the local changes are properly reflected in OneDrive, while for downloads, the sync client needs to ensure that the remote changes are accurately applied to the local file system. The Direction property allows the sync client to manage synchronization operations effectively based on the intended flow of data between the local system and OneDrive.
    /// </summary>
    public SyncDirection Direction { get; set; }

    /// <summary>
    /// The current state of the synchronization job, represented by the SyncJobState enumeration. This property is used to track the progress and status of the sync job, allowing for efficient management of synchronization tasks and handling of any errors that may occur during the process. The State property can be updated as the sync job progresses through different stages, such as Queued, InProgress, Completed, or Failed, providing valuable information for both the sync client application and the user interface to reflect the current status of synchronization operations.
    /// </summary>
    public SyncJobState State { get; set; } = SyncJobState.Queued;

    /// <summary>
    /// The error message associated with the synchronization job, if any. This property is used to capture details about any errors that occur during the synchronization process, allowing for effective troubleshooting and user feedback. If the sync job encounters an error, the ErrorMessage can be populated with relevant information about the issue, such as the nature of the error, potential causes, and suggested resolutions. This information can then be displayed in the user interface or logged for further analysis, helping users and developers to understand and address synchronization issues more effectively.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The timestamp when the synchronization job was queued. This is used to track when the sync job was created and to manage the scheduling of synchronization operations. By recording the QueuedAt timestamp, the sync client can determine how long a sync job has been waiting to be processed and can prioritize jobs accordingly, ensuring that synchronization tasks are handled in a timely manner based on their creation time.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// The size of the item being synchronized in bytes. This is used to track the size of the item and to manage synchronization operations based on the size, such as determining whether to perform a sync operation immediately or to defer it based on the item's size and available resources. By recording the FileSize, the sync client can make informed decisions about how to handle synchronization tasks, especially for larger items that may require more time and bandwidth to synchronize effectively.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// The timestamp of the last modification of the item in OneDrive, provided by the Microsoft Graph API. This is used to track changes to the item in OneDrive and to determine whether synchronization operations are needed based on the modification time. By comparing the RemoteModified timestamp with the local modification time, the sync client can identify when an item has been changed remotely and needs to be updated locally, or when a conflict has occurred due to concurrent modifications both locally and remotely. This property is essential for maintaining data consistency and ensuring that the most up-to-date version of the item is available in both locations after synchronization.
    /// </summary>
    public DateTimeOffset RemoteModified { get; set; }

    /// <summary>
    /// The timestamp when the synchronization job was queued. This is used to track when the sync job was created and to manage the scheduling of synchronization operations. By recording the QueuedAt timestamp, the sync client can determine how long a sync job has been waiting to be processed and can prioritize jobs accordingly, ensuring that synchronization tasks are handled in a timely manner based on their creation time.
    /// </summary>
    public DateTimeOffset QueuedAt { get; set; }

    /// <summary>
    /// The timestamp when the synchronization job started processing. This is used to track the duration of the sync job and to manage the scheduling of synchronization operations. By recording the StartedAt timestamp, the sync client can determine how long a sync job has been in progress and can provide feedback to users about the status of their synchronization tasks, as well as identify potential performance issues or bottlenecks in the synchronization process.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The timestamp when the synchronization job completed processing. This is used to track the duration of the sync job and to manage the scheduling of synchronization operations. By recording the CompletedAt timestamp, the sync client can determine how long a sync job took to complete and can provide feedback to users about the status of their synchronization tasks, as well as identify potential performance issues or bottlenecks in the synchronization process.
    /// </summary>
    public AccountEntity? Account { get; set; }
}
