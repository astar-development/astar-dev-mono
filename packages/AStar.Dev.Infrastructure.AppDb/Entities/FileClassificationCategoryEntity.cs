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

    /// <summary>FK to parent category; null for root nodes.</summary>
    public int? ParentId { get; set; }

    /// <summary>Whether this keyword defines a famous person.</summary>
    public bool IsFamous { get; set; }

    /// <summary>Whether this keyword defines the internet classification.</summary>
    public bool IsInternet { get; set; }

    /// <summary>Whether files matching this category should be included in search results.</summary>
    public bool IncludeInSearch { get; set; }

    /// <summary>Navigation to parent category.</summary>
    public FileClassificationCategoryEntity? Parent { get; set; }
}
