namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Junction row linking a file to a category in the normalised classification taxonomy, regardless of which application classified it.</summary>
public sealed class FileClassificationEntity
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
