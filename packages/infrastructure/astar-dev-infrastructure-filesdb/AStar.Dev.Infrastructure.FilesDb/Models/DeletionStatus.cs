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
    ///     Gets or sets when the file was 'soft deleted'. I know, shocking...
    /// </summary>
    public DateTimeOffset? SoftDeleted { get; set; }

    /// <summary>
    ///     Gets or sets when the file was marked as 'soft delete pending'. I know, shocking...
    /// </summary>
    public DateTimeOffset? SoftDeletePending { get; set; }

    /// <summary>
    ///     Gets or sets when the file was marked as 'hard delete pending'. I know, shocking...
    /// </summary>
    public DateTimeOffset? HardDeletePending { get; set; }
}