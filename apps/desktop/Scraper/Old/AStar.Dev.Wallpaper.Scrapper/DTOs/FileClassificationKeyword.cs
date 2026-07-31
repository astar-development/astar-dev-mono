namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

/// <summary>
///     Represents a keyword matched against file names to apply a <see cref="FileClassification" />.
/// </summary>
public sealed class FileClassificationKeyword
{
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
