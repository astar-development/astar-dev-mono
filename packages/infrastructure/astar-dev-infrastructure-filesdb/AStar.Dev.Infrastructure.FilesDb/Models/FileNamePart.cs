using AStar.Dev.Infrastructure.Data;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Represents a segment or part of a file name, which can be stored in the database.
///     Provides properties to define the text of the file name part,
///     whether it should be included in searches, and the associated classifications.
/// </summary>
public class FileNamePart : AuditableEntity
{
    /// <summary>
    ///     Gets or sets the unique identifier for the <see cref="FileNamePart" /> entity.
    ///     This property serves as the primary key in the database to distinguish
    ///     each record of file name parts.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this classification should be included in search results.
    ///     This property determines if files associated with this classification are considered searchable.
    /// </summary>
    public bool IncludeInSearch { get; set; }
}