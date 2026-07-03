namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>A keyword matched against path tokens to classify files into a category.</summary>
public sealed class FileClassificationKeywordEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Keyword matched against path tokens (e.g. "photos").</summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>FK to the owning category.</summary>
    public int CategoryId { get; set; }

    /// <summary>Whether this keyword overrides the famous classification flag.</summary>
    public bool IsFamous { get; set; }

    /// <summary>Whether this keyword overrides the internet classification flag.</summary>
    public bool IsInternet { get; set; }

    /// <summary>Navigation to the owning category.</summary>
    public FileClassificationCategoryEntity? Category { get; set; }
}
