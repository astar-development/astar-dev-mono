namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

/// <summary>
///     Represents a keyword matched against file names to apply a <see cref="FileClassification" />.
/// </summary>
public sealed class FileClassificationKeyword
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
    ///     Gets or sets the unique identifier for the <see cref="FileClassificationKeyword" /> entity.
    ///     This property serves as the primary key in the database to distinguish
    ///     each record of classification keywords.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the text of the keyword matched against file names.
    /// </summary>
    public string Keyword { get; set; } = string.Empty;
}
