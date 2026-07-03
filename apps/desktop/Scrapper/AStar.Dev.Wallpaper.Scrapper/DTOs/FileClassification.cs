namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

/// <summary>
///     Represents a classification of files, providing metadata about the type of files
///     and associated entities such as file details and file name parts.
/// </summary>
public sealed class FileClassification
{
    /// <summary>
    ///     Gets or sets the unique identifier for the file classification.
    ///     This property serves as the primary key for the <see cref="FileClassification" /> entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the file classification.
    ///     This property represents the descriptive label for a specific classification
    ///     and is often used to identify or categorize files within the database.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the hierarchy level: 1 = top, 2 = sub, 3 = leaf.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    ///     Gets or sets the id of the parent classification; null for root nodes.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the file classification is considered "famous."
    ///     This property is used to mark specific classifications with special significance.
    /// </summary>
    public bool IsFamous { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this classification should be included in search results.
    ///     This property determines if files associated with this classification are considered searchable.
    /// </summary>
    public bool IncludeInSearch { get; set; }

    /// <summary>
    ///     Gets or sets the collection of keywords associated with the file classification.
    ///     This property represents the one-to-many relationship between a file classification
    ///     and the keywords matched against file names to apply it.
    /// </summary>
    public List<FileClassificationKeyword> Keywords { get; set; } = [];
}
