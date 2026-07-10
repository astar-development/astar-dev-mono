using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scraper.DTOs;

/// <summary>
///     Represents a unique tag observed during a scrape run
/// </summary>
public sealed class ScrapedTag
{
    /// <summary>
    ///     Gets or sets the Id of the <see cref="ScrapedTag" />
    /// </summary>
    public ScrapedTagId Id { get; set; } = ScrapedTagId.CreateNew();

    /// <summary>
    ///     Gets or sets the tag text value (unique)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the category for the tag.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the tag should be included in search results.
    /// </summary>
    public bool IncludeInSearch { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tag is famous.
    /// </summary>
    public bool IsFamous { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tag is related to the internet.
    /// </summary>
    public bool IsInternet { get; set; }

    /// <summary>
    ///    Gets or sets the level of the tag in the hierarchy. Level 1 is the top level, level 2 is a sub-level, and level 3 is a leaf level.
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    ///   Gets or sets the date and time when this tag was created. This is set automatically when the entity is first persisted to the database and should not be modified thereafter.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///   Gets or sets the date and time when this tag was last updated. This is set automatically when the entity is updated in the database.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public override string ToString() => this.ToJson();
}
