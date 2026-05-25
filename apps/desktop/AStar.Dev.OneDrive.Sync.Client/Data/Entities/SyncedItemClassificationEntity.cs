using System.ComponentModel.DataAnnotations.Schema;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>Stores a single classification tag for a synced file item.</summary>
public sealed class SyncedItemClassificationEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="SyncedItemEntity"/>.</summary>
    public int SyncedItemId { get; set; }

    /// <summary>The top-level classification category.</summary>
    public string Level1 { get; set; } = string.Empty;

    /// <summary>The second-level classification category, or null if not applicable.</summary>
    public string? Level2 { get; set; }

    /// <summary>The third-level classification category, or null if not applicable.</summary>
    public string? Level3 { get; set; }

    /// <summary>The most-specific classification level: Level3 if present, Level2 if present, otherwise Level1.</summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>Indicates whether this classification is flagged as special.</summary>
    public bool IsSpecial { get; set; }

    /// <summary>Navigation property to the owning synced item.</summary>
    [ForeignKey(nameof(SyncedItemId))]
    public SyncedItemEntity? SyncedItem { get; set; }
}
