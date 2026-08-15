namespace AStarDev.ControlDb.Files;

/// <summary>Tracks the soft- and hard-deletion state of a file.</summary>
/// <param name="Id">Primary key.</param>
/// <param name="FileEntityId">Foreign key to the parent <see cref="FileEntity"/>.</param>
/// <param name="SoftDeleted">The date the file was soft-deleted, or null if not soft-deleted.</param>
/// <param name="SoftDeletePending">The date the file is pending soft-deletion, or null if none pending.</param>
/// <param name="HardDeletePending">The date the file is pending hard-deletion, or null if none pending.</param>
public sealed record DeletionStatusEntity(DeletionStatusId Id, FileId FileEntityId, DateTimeOffset? SoftDeleted, DateTimeOffset? SoftDeletePending, DateTimeOffset? HardDeletePending);
