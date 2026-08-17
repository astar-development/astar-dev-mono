namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Base class providing creation and modification timestamps for auditable entities.</summary>
public abstract class AuditableEntity
{
    /// <summary>The date and time the entity was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The date and time the entity was last modified.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
