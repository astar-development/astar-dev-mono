namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///    The <see cref="AuditableEntity"></see> class provides common properties for tracking the creation and modification of entities.
/// </summary>
public class AuditableEntity
{
    /// <summary>
    ///     Gets or sets the date and time when the entity was created.
    ///     This property is automatically set when a new instance of the entity is added to the database.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the date and time when the entity was last modified.
    ///     This property is automatically updated whenever changes are made to the entity and saved to the database.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}