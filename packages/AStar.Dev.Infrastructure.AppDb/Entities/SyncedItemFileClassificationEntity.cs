namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Junction row linking a classified file to a category in the normalised classification taxonomy. Exactly one parent is set: <see cref="SyncedItemId"/> for OneDrive-synced items, <see cref="FileDetailId"/> for scraper-downloaded files.</summary>
public sealed class SyncedItemFileClassificationEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="SyncedItemEntity"/>; null when the row classifies a downloaded file.</summary>
    public int? SyncedItemId { get; set; }

    /// <summary>Foreign key to the owning <see cref="FileDetailEntity"/>; null when the row classifies a synced item.</summary>
    public FileId? FileDetailId { get; set; }

    /// <summary>Foreign key to the <see cref="FileClassificationCategoryEntity"/> that classifies this item.</summary>
    public int CategoryId { get; set; }

    /// <summary>Navigation property to the owning synced item.</summary>
    public SyncedItemEntity? SyncedItem { get; set; }

    /// <summary>Navigation property to the owning downloaded file.</summary>
    public FileDetailEntity? FileDetail { get; set; }

    /// <summary>Navigation property to the classification category.</summary>
    public FileClassificationCategoryEntity? Category { get; set; }
}
