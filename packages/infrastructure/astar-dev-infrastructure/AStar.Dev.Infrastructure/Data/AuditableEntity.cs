namespace AStar.Dev.Infrastructure.Data;

/// <summary>
///     The <see cref="AuditableEntity" /> class defines the audit properties to be set on all relevant entities
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>
    ///     Gets or sets the Updated By property to track who made the change
    /// </summary>
    public string UpdatedBy { get; set; } = "Jay Barden";

    /// <summary>
    ///     Gets or sets the date and time of the update. This is specified in UTC
    /// </summary>
    public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.UtcNow;
}
