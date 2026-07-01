namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

/// <summary>
///     Represents a classification of files, providing metadata about the type of files
///     and associated entities such as file details and file name parts.
/// </summary>
public sealed class FileClassification
{
    /// <summary>
    ///     Gets or sets the date and time when the entity was created.
    ///     This property is automatically set when a new instance of the entity is added to the database.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the entity was last modified.
    ///     This property is automatically updated whenever changes are made to the entity and saved to the database.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

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
    ///     Gets or sets a value indicating whether the file classification is considered a "Celebrity."
    ///     This property is used to mark specific classifications with special significance.
    /// </summary>
    public bool Celebrity { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this classification should be included in search results.
    ///     This property determines if files associated with this classification are considered searchable.
    /// </summary>
    public bool IncludeInSearch { get; set; }

    /// <summary>
    ///     Gets or sets the collection of file name parts associated with the file classification.
    ///     This property represents the one-to-many relationship between a file classification
    ///     and its constituent parts that define or describe its naming structure.
    /// </summary>
    public List<FileNamePart> FileNameParts { get; set; } = [];
}
