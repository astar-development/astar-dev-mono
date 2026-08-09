namespace AStarDev.FilesDb;

/// <summary>Tracks the soft- and hard-deletion state of a file.</summary>
public sealed class DeletionStatusEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the parent <see cref="FileEntity"/>.</summary>
    public FileId FileEntityId { get; set; }

    /// <summary>Navigation property to the parent file entity.</summary>
    public FileEntity FileEntity { get; set; } = null!;

    /// <summary>The date the file was soft-deleted, or null if not soft-deleted.</summary>
    public DateTimeOffset? SoftDeleted { get; set; }

    /// <summary>The date the file is pending soft-deletion, or null if none pending.</summary>
    public DateTimeOffset? SoftDeletePending { get; set; }

    /// <summary>The date the file is pending hard-deletion, or null if none pending.</summary>
    public DateTimeOffset? HardDeletePending { get; set; }
}
