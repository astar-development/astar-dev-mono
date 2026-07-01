namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Defines dates/times for soft and hard deletion
/// </summary>
public sealed class DeletionStatus
{
    /// <summary>
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///
    /// </summary>
    public DateTimeOffset? SoftDeleted { get; set; }

    /// <summary>
    ///
    /// </summary>
    public DateTimeOffset? SoftDeletePending { get; set; }

    /// <summary>
    ///
    /// </summary>
    public DateTimeOffset? HardDeletePending { get; set; }
}

