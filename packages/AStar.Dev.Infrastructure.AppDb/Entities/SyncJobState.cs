namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Defines the various states that a synchronization job can be in during its lifecycle, including Queued, InProgress, Completed, Failed, and Skipped. This enumeration is used to track and manage the status of sync operations within the sync client, allowing for appropriate handling of each state and providing feedback to the user about the progress of their synchronization tasks.
/// </summary>
public enum SyncJobState
{
    /// <summary>
    /// Indicates that the synchronization job has been created and is waiting to be processed. Jobs in this state are typically queued for execution and have not yet started the synchronization process.
    /// </summary>
    Queued,

    /// <summary>
    /// Indicates that the synchronization job is currently being processed. Jobs in this state are actively synchronizing files between the local system and OneDrive, and their progress can be monitored to provide feedback to the user. This state is crucial for managing the execution of sync tasks and ensuring that resources are allocated appropriately during the synchronization process.
    /// </summary>
    InProgress,

    /// <summary>
    /// Indicates that the synchronization job has been completed successfully. Jobs in this state have finished processing and all files have been synchronized between the local system and OneDrive.
    /// </summary>
    Completed,

    /// <summary>
    /// Indicates that the synchronization job has failed. Jobs in this state have encountered an error during processing and need to be retried or handled manually.
    /// </summary>
    Failed,

    /// <summary>
    /// Indicates that the synchronization job has been skipped. Jobs in this state are not processed and are typically skipped due to certain conditions or user actions.
    /// </summary>
    Skipped
}
