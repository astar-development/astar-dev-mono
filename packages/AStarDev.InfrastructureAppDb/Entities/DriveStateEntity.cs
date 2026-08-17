using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the synchronization state of a OneDrive account, including the latest delta link and last sync timestamp.
/// </summary>
public sealed class DriveStateEntity
{
    /// <summary>Primary key for the drive state record.</summary>
    public int Id { get; set; }

    /// <summary>The identifier of the OneDrive account associated with this drive state.</summary>
    public AccountId AccountId { get; set; }

    /// <summary>The delta link for tracking incremental changes, or None before the first sync.</summary>
    public Option<string> DeltaLink { get; set; } = Option.None<string>();

    /// <summary>Timestamp of the last sync operation start, or None if never synced.</summary>
    public Option<DateTimeOffset> LastSyncStartedAt { get; set; } = Option.None<DateTimeOffset>();

    /// <summary>Navigation property to the associated AccountEntity.</summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
