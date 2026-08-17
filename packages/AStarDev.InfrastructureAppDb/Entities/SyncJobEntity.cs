using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents a synchronization job for a specific OneDrive item.
/// </summary>
public sealed class SyncJobEntity
{
    /// <summary>Unique identifier for the synchronization job.</summary>
    public Guid Id { get; set; }

    /// <summary>The identifier of the OneDrive account associated with this sync job.</summary>
    public AccountId AccountId { get; set; }

    /// <summary>The identifier of the OneDrive folder containing the item being synchronized.</summary>
    public OneDriveFolderId FolderId { get; set; }

    /// <summary>The identifier of the item being synchronized in OneDrive.</summary>
    public OneDriveItemId RemoteItemId { get; set; }

    /// <summary>The relative path of the item within the OneDrive folder structure.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>The local path of the item on the user's file system.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>The direction of the synchronization operation.</summary>
    public SyncDirection Direction { get; set; }

    /// <summary>The current state of the synchronization job.</summary>
    public SyncJobState State { get; set; } = SyncJobState.Queued;

    /// <summary>Error message if the job failed, or None if no error occurred.</summary>
    public Option<string> ErrorMessage { get; set; } = Option.None<string>();

    /// <summary>Pre-authenticated download URL for the item, or None for uploads and deletes.</summary>
    public Option<string> DownloadUrl { get; set; } = Option.None<string>();

    /// <summary>The size of the item being synchronized in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>The remote last-modified timestamp.</summary>
    public DateTimeOffset RemoteModified { get; set; }

    /// <summary>Timestamp when the job was queued.</summary>
    public DateTimeOffset QueuedAt { get; set; }

    /// <summary>Timestamp when the job completed, or None if still in progress.</summary>
    public Option<DateTimeOffset> CompletedAt { get; set; } = Option.None<DateTimeOffset>();

    /// <summary>Navigation property to the associated AccountEntity.</summary>
    public AccountEntity? Account { get; set; }
}
