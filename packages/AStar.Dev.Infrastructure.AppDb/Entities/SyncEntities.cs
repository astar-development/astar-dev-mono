using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents a synchronization conflict for a specific file between the local system and OneDrive.
/// </summary>
public sealed class SyncConflictEntity
{
    /// <summary>Unique identifier for the synchronization conflict.</summary>
    public Guid Id { get; set; }

    /// <summary>The identifier of the OneDrive account associated with this conflict.</summary>
    public AccountId AccountId { get; set; }

    /// <summary>The identifier of the OneDrive folder containing the conflicting item.</summary>
    public OneDriveFolderId FolderId { get; set; }

    /// <summary>The identifier of the conflicting item in OneDrive.</summary>
    public OneDriveItemId RemoteItemId { get; set; }

    /// <summary>The relative path of the conflicting item within the OneDrive folder structure.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>The local path of the conflicting item on the user's file system.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>The timestamp of the last local modification.</summary>
    public DateTimeOffset LocalModified { get; set; }

    /// <summary>The timestamp of the last remote modification.</summary>
    public DateTimeOffset RemoteModified { get; set; }

    /// <summary>The size of the item on the local file system in bytes.</summary>
    public long LocalSize { get; set; }

    /// <summary>The size of the item in OneDrive in bytes.</summary>
    public long RemoteSize { get; set; }

    /// <summary>The current state of the conflict.</summary>
    public ConflictState State { get; set; } = ConflictState.Pending;

    /// <summary>The resolution policy applied to the conflict, or None if not yet resolved.</summary>
    public Option<ConflictPolicy> Resolution { get; set; } = Option.None<ConflictPolicy>();

    /// <summary>The timestamp when the conflict was detected.</summary>
    public DateTimeOffset DetectedAt { get; set; }

    /// <summary>The timestamp when the conflict was resolved, or None if still pending.</summary>
    public Option<DateTimeOffset> ResolvedAt { get; set; } = Option.None<DateTimeOffset>();

    /// <summary>Navigation property to the associated AccountEntity.</summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
