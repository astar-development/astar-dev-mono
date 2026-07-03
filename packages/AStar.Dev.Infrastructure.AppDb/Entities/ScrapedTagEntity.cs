namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>A unique tag observed during a scrape run.</summary>
public sealed class ScrapedTagEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public ScrapedTagId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The tag text value (unique).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>The category for the tag.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Whether files with this tag should be included in search results.</summary>
    public bool IncludeInSearch { get; set; }
}
