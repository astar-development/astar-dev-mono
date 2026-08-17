namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Persisted category node in the file classification hierarchy.</summary>
public sealed class FileClassificationCategoryEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Category name (e.g. "Photos", "Documents").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hierarchy level: 1 = top, 2 = sub, 3 = leaf.</summary>
    public int Level { get; set; }

    /// <summary>
    ///   Priority for ordering categories at the same level. Lower values are higher priority. Default is 1.
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>FK to parent category; null for root nodes.</summary>
    public int? ParentId { get; set; }

    /// <summary>Whether this category defines a famous person.</summary>
    public bool IsFamous { get; set; }

    /// <summary>Whether this category defines the internet classification.</summary>
    public bool IsInternet { get; set; }

    /// <summary>Whether files matching this category should be included in search results.</summary>
    public bool IncludeInSearch { get; set; }

    /// <summary>
    ///  The date and time when this category was created. This is set automatically when the entity is first persisted to the database and should not be modified thereafter.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time when this category was last updated. This is set automatically whenever the entity is modified and persisted to the database. It can be null if the entity has never been updated since creation.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Whether this category is active and should be considered in operations.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation to parent category.</summary>
    public FileClassificationCategoryEntity? Parent { get; set; }
}
