namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>Junction row linking a downloaded file to a category in the normalised classification taxonomy.</summary>
public sealed class DownloadedFileClassificationEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="FileDetailEntity"/>.</summary>
    public FileId FileDetailId { get; set; }

    /// <summary>Foreign key to the <see cref="FileClassificationCategoryEntity"/> that classifies this file.</summary>
    public int CategoryId { get; set; }

    /// <summary>Navigation property to the owning file.</summary>
    public FileDetailEntity? FileDetail { get; set; }

    /// <summary>Navigation property to the classification category.</summary>
    public FileClassificationCategoryEntity? Category { get; set; }
}
