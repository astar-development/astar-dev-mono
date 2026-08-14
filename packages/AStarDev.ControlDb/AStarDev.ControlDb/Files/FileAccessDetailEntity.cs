namespace AStarDev.ControlDb.Files;

/// <summary>Tracks when a file's details were last refreshed and when it was last viewed.</summary>
/// <param name="FileEntityId">Foreign key to the parent <see cref="FileEntity"/>.</param>
/// <param name="Id">Primary key.</param>
/// <param name="DetailsLastUpdated">The date the file's details were last updated, or null if never updated.</param>
/// <param name="LastViewed">The date the file was last viewed, or null if never viewed.</param>
/// <param name="MoveRequired">Whether the file has been marked as needing to move.</param>
public sealed record FileAccessDetailEntity(FileAccessDetailId Id, FileId FileEntityId, DateTimeOffset? DetailsLastUpdated, DateTimeOffset? LastViewed, bool MoveRequired);
