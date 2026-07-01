using AStar.Dev.Utilities;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Represents a unique tag observed during a scrape run
/// </summary>
public sealed class ScrapedTag : AuditableEntity
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
    public string Category {get;set;} = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public bool IncludeInSearch { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => this.ToJson();
}
