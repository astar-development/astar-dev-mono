namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>Junction row linking a synced file item to a category in the normalised classification taxonomy.</summary>
public sealed class SyncedItemFileClassificationEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="SyncedItemEntity"/>.</summary>
    public int SyncedItemId { get; set; }

    /// <summary>Foreign key to the <see cref="FileClassificationCategoryEntity"/> that classifies this item.</summary>
    public int CategoryId { get; set; }

    /// <summary>Navigation property to the owning synced item.</summary>
    public SyncedItemEntity? SyncedItem { get; set; }

    /// <summary>Navigation property to the classification category.</summary>
    public FileClassificationCategoryEntity? Category { get; set; }
}
